using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services;
using GhostLang.Core.Settings.Asr;

namespace GhostLang.Core.Services.AudioCapture;

public class AudioTranslationManager(
    IAudioCaptureServiceFactory captureFactory,
    IConfigurationService configService,
    IPipelineBuilder pipelineBuilder) : IAudioTranslationManager
{
    private IAudioCaptureService? _captureService;
    private readonly List<byte> _buffer = new();
    private readonly object _bufferLock = new();
    private readonly TimeSpan _chunkDuration = TimeSpan.FromSeconds(5);
    private volatile bool _isProcessing;
    private SupportedLanguage _targetLanguage;
    private List<SupportedLanguage> _sourceLanguages = new();
    private int _sampleRate = 16000;
    private int _channelCount = 1;

    public bool IsActive => _captureService is { IsCapturing: true };

    public event EventHandler<AudioTranslationSessionEventArgs>? FragmentsReady;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<float>? LevelChanged;

    public async Task StartAsync(AudioCaptureSource source, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguages)
    {
        if (IsActive)
            return;

        _targetLanguage = targetLanguage;
        _sourceLanguages = sourceLanguages;

        _captureService = captureFactory.Create(source);
        _sampleRate = _captureService.SampleRate;
        _channelCount = _captureService.ChannelCount;
        _captureService.ChunkCaptured += OnChunkCaptured;

        lock (_bufferLock) _buffer.Clear();

        await _captureService.StartAsync();
        StatusChanged?.Invoke(this, "Active");
    }

    public async Task StopAsync()
    {
        if (_captureService is null)
            return;

        _captureService.ChunkCaptured -= OnChunkCaptured;
        await _captureService.StopAsync();
        _captureService.Dispose();
        _captureService = null;

        lock (_bufferLock) _buffer.Clear();

        StatusChanged?.Invoke(this, "Stopped");
    }

    private void OnChunkCaptured(object? sender, AudioChunkCapturedEventArgs e)
    {
        LevelChanged?.Invoke(this, e.LevelDb);

        int currentSize;
        lock (_bufferLock)
        {
            _buffer.AddRange(e.PcmData);
            currentSize = _buffer.Count;
        }

        var bytesForDuration = (int)(_chunkDuration.TotalSeconds * _sampleRate * _channelCount * 2);

        if (currentSize >= bytesForDuration && !_isProcessing)
        {
            _ = FlushAndProcessAsync();
        }
    }

    private async Task FlushAndProcessAsync()
    {
        if (_isProcessing)
            return;

        _isProcessing = true;

        byte[] pcm;
        lock (_bufferLock)
        {
            pcm = _buffer.ToArray();
            _buffer.Clear();
        }

        try
        {
            var config = configService.Load();
            if (config.ActiveAsrEngine is null)
            {
                config.ActiveAsrEngine = new WhisperAsrOptions();
            }

            var pipeline = pipelineBuilder.BuildAudioPipeline(config);
            var context = await pipeline.ProcessAsync(pcm, _sampleRate, _channelCount, _targetLanguage, _sourceLanguages);

            if (context.AudioFragments.Count > 0)
            {
                FragmentsReady?.Invoke(this, new AudioTranslationSessionEventArgs
                {
                    Fragments = context.AudioFragments
                });
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
}