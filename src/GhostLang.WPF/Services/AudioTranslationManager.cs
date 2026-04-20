using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services;
using GhostLang.Core.Services.AudioCapture;
using GhostLang.Core.Settings.Asr;

namespace GhostLang.WPF.Services;

public class AudioTranslationManager : IAudioTranslationManager
{
    private readonly IAudioCaptureServiceFactory _captureFactory;
    private readonly IConfigurationService _configService;
    private readonly IPipelineBuilder _pipelineBuilder;

    private IAudioCaptureService? _captureService;

    private const int MaxBufferCapacityBytes = 48_000 * 2 * 2 * 8;
    private readonly List<byte> _buffer = new(MaxBufferCapacityBytes);

    public AudioTranslationManager(
        IAudioCaptureServiceFactory captureFactory,
        IConfigurationService configService,
        IPipelineBuilder pipelineBuilder)
    {
        _captureFactory = captureFactory;
        _configService = configService;
        _pipelineBuilder = pipelineBuilder;
    }

    private readonly object _bufferLock = new();
    private readonly TimeSpan _chunkDuration = TimeSpan.FromSeconds(5);

    private readonly TimeSpan _maxBufferDuration = TimeSpan.FromSeconds(7.5);

    private readonly SemaphoreSlim _processingGate = new(1, 1);

    private volatile bool _isStopping;

    private SupportedLanguage _targetLanguage;
    private List<SupportedLanguage> _sourceLanguages = new();
    private int _sampleRate = 16000;
    private int _channelCount = 1;

    private IAudioTranslationPipeline? _pipeline;

    private long? _bufferStartMs;

    public bool IsActive => _captureService is { IsCapturing: true };

    public event EventHandler<AudioTranslationSessionEventArgs>? FragmentsReady;

    public event EventHandler<PipelineStatus>? StatusChanged;

    public event EventHandler<float>? LevelChanged;

    public event EventHandler<long>? DriftChanged;

    private long? _lastProcessedEndMs;

    private long _lastDriftEmitMs;

    private long _droppedSampleBytes;

    private long _lastDropNotifyTicks;

    public async Task StartAsync(AudioCaptureSource source, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguages)
    {
        if (IsActive)
            return;

        _isStopping = false;
        _lastProcessedEndMs = null;
        _lastDriftEmitMs = 0;
        _droppedSampleBytes = 0;
        _lastDropNotifyTicks = 0;
        _targetLanguage = targetLanguage;
        _sourceLanguages = sourceLanguages;

        _captureService = _captureFactory.Create(source);
        _sampleRate = _captureService.SampleRate;
        _channelCount = _captureService.ChannelCount;
        _captureService.ChunkCaptured += OnChunkCaptured;
        _captureService.SamplesDropped += OnSamplesDropped;

        lock (_bufferLock)
        {
            _buffer.Clear();
            _bufferStartMs = null;
        }

        var config = _configService.Load();
        if (config.ActiveAsrEngine is null)
            config.ActiveAsrEngine = new WhisperAsrOptions();

        _pipeline?.Dispose();
        _pipeline = _pipelineBuilder.BuildAudioPipeline(config);

        await _captureService.StartAsync();
        StatusChanged?.Invoke(this, new PipelineStatus.Active());
    }

    public async Task StopAsync()
    {
        if (_captureService is null)
            return;

        _isStopping = true;
        _captureService.ChunkCaptured -= OnChunkCaptured;
        _captureService.SamplesDropped -= OnSamplesDropped;
        await _captureService.StopAsync();
        _captureService.Dispose();
        _captureService = null;

        var acquired = await _processingGate.WaitAsync(TimeSpan.FromSeconds(3));
        try
        {
            lock (_bufferLock)
            {
                _buffer.Clear();
                _bufferStartMs = null;
            }
        }
        finally
        {
            if (acquired) _processingGate.Release();
        }

        _pipeline?.Dispose();
        _pipeline = null;

        StatusChanged?.Invoke(this, new PipelineStatus.Stopped());
    }

    private void OnSamplesDropped(object? sender, int droppedBytes)
    {
        if (_isStopping) return;

        Interlocked.Add(ref _droppedSampleBytes, droppedBytes);

        var nowTicks = DateTime.UtcNow.Ticks;
        var lastTicks = Interlocked.Read(ref _lastDropNotifyTicks);
        if (nowTicks - lastTicks < TimeSpan.TicksPerSecond) return;
        Interlocked.Exchange(ref _lastDropNotifyTicks, nowTicks);

        var totalBytes = Interlocked.Read(ref _droppedSampleBytes);
        var bytesPerSecond = Math.Max(1, _sampleRate * _channelCount * 2);
        var totalMs = totalBytes * 1000 / bytesPerSecond;
        StatusChanged?.Invoke(this, new PipelineStatus.CaptureOverflow(totalMs));
    }

    private void OnChunkCaptured(object? sender, AudioChunkCapturedEventArgs e)
    {
        if (_isStopping) return;

        LevelChanged?.Invoke(this, e.LevelDb);

        if (_lastProcessedEndMs.HasValue && e.TimestampMs - _lastDriftEmitMs >= 500)
        {
            var drift = Math.Max(0, e.TimestampMs - _lastProcessedEndMs.Value);
            DriftChanged?.Invoke(this, drift);
            _lastDriftEmitMs = e.TimestampMs;
        }

        var bytesPerSecond = _sampleRate * _channelCount * 2;
        var maxBufferBytes = (int)(_maxBufferDuration.TotalSeconds * bytesPerSecond);

        int currentSize;
        lock (_bufferLock)
        {

            if (_buffer.Count == 0 && _bufferStartMs is null)
                _bufferStartMs = e.TimestampMs;

            _buffer.AddRange(e.PcmData);

            if (_buffer.Count > maxBufferBytes)
            {
                var overflow = _buffer.Count - maxBufferBytes;
                _buffer.RemoveRange(0, overflow);

                if (_bufferStartMs.HasValue)
                {
                    var droppedMs = (long)(overflow * 1000.0 / bytesPerSecond);
                    _bufferStartMs = _bufferStartMs.Value + droppedMs;
                }
            }

            currentSize = _buffer.Count;
        }

        var bytesForDuration = (int)(_chunkDuration.TotalSeconds * bytesPerSecond);

        if (currentSize >= bytesForDuration)
        {
            _ = FlushAndProcessAsync();
        }
    }

    private async Task FlushAndProcessAsync()
    {

        if (!await _processingGate.WaitAsync(0))
            return;

        try
        {
            byte[] pcm;
            long? captureStartMs;
            lock (_bufferLock)
            {
                if (_buffer.Count == 0) return;
                pcm = _buffer.ToArray();
                _buffer.Clear();
                captureStartMs = _bufferStartMs;
                _bufferStartMs = null;
            }

            var pipeline = _pipeline;
            if (pipeline is null) return;

            var context = await pipeline.ProcessAsync(pcm, _sampleRate, _channelCount, _targetLanguage, _sourceLanguages, captureStartMs);

            if (captureStartMs.HasValue)
            {
                var bytesPerSecond = _sampleRate * _channelCount * 2;
                var chunkDurationMs = (long)(pcm.Length * 1000.0 / bytesPerSecond);
                _lastProcessedEndMs = captureStartMs.Value + chunkDurationMs;
            }

            if (!_isStopping && context.AudioFragments.Count > 0)
            {
                FragmentsReady?.Invoke(this, new AudioTranslationSessionEventArgs
                {
                    Fragments = context.AudioFragments
                });
            }
        }
        catch (Exception ex)
        {
            if (!_isStopping)
                StatusChanged?.Invoke(this, new PipelineStatus.Error(ex.Message, ex));
        }
        finally
        {
            _processingGate.Release();
        }
    }
}
