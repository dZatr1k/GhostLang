using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;
using GhostLang.Core.Services.Asr;
using GhostLang.Core.Services.AudioCapture;
using Whisper.net;
using Whisper.net.LibraryLoader;

namespace GhostLang.Core.Settings.Asr;

public class WhisperAsrEngine : IAsrEngine, IDisposable
{
    private readonly WhisperAsrOptions _options;
    private readonly IWhisperModelManager _modelManager;

    private readonly object _cacheLock = new();
    private WhisperFactory? _cachedFactory;
    private string? _cachedFactoryPath;
    private WhisperProcessor? _cachedProcessor;
    private string? _cachedProcessorLang;

    public WhisperAsrEngine(WhisperAsrOptions options, IWhisperModelManager modelManager)
    {
        _options = options;
        _modelManager = modelManager;
    }

    public IReadOnlySet<SupportedLanguage> SupportedLanguages => LanguageCapabilitySets.AllTwenty;

    public bool SupportsStreaming => false;

    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        return Task.FromResult(language != SupportedLanguage.Unknown);
    }

    public async Task<List<AudioFragment>> RecognizeAsync(AudioTranslationContext context, List<SupportedLanguage> languages)
    {
        if (context.OriginalAudio is null || context.OriginalAudio.Length == 0)
            return new List<AudioFragment>();

        var modelPath = await _modelManager.EnsureModelAsync(_options.ModelName, _options.ModelsPath);

        var fileInfo = new FileInfo(modelPath);
        if (fileInfo.Length < 30_000_000)
        {
            throw new InvalidOperationException(
                $"Whisper model file '{_options.ModelName}' is suspiciously small ({fileInfo.Length / 1024 / 1024} MB). " +
                "Delete it in Settings and re-download.");
        }

        var sourceLang = languages.FirstOrDefault(l => l != SupportedLanguage.Unknown);

        var langCode = sourceLang.ToWhisperLanguageCode();

        var processor = GetOrCreateProcessor(modelPath, langCode, fileInfo);
        return await ProcessInternalAsync(context, processor);
    }

    private WhisperProcessor GetOrCreateProcessor(string modelPath, string langCode, FileInfo modelFileInfo)
    {
        lock (_cacheLock)
        {
            var factoryChanged = _cachedFactory is null || _cachedFactoryPath != modelPath;
            var processorChanged = _cachedProcessor is null || _cachedProcessorLang != langCode || factoryChanged;

            if (factoryChanged)
            {
                _cachedProcessor?.Dispose();
                _cachedProcessor = null;
                _cachedFactory?.Dispose();

                ConfigureRuntimeLibraryOrder(_options.GpuRuntime);

                try
                {
                    _cachedFactory = WhisperFactory.FromPath(modelPath);
                    _cachedFactoryPath = modelPath;
                }
                catch (Exception ex)
                {
                    _cachedFactory = null;
                    _cachedFactoryPath = null;
                    throw new InvalidOperationException(
                        $"Failed to load Whisper model '{_options.ModelName}' ({modelFileInfo.Length / 1024 / 1024} MB): {ex.Message}. " +
                        "Try a smaller model (base/small) or re-download.",
                        ex);
                }
            }

            if (processorChanged)
            {
                _cachedProcessor?.Dispose();
                try
                {
                    _cachedProcessor = _cachedFactory!.CreateBuilder()
                        .WithLanguage(langCode)
                        .Build();
                    _cachedProcessorLang = langCode;
                }
                catch (Exception ex)
                {
                    _cachedProcessor = null;
                    _cachedProcessorLang = null;
                    throw new InvalidOperationException(
                        $"Failed to initialize Whisper processor (model '{_options.ModelName}', language '{langCode}'): {ex.Message}. " +
                        "This often means insufficient memory for large models — try base or small.",
                        ex);
                }
            }

            return _cachedProcessor!;
        }
    }

    private static async Task<List<AudioFragment>> ProcessInternalAsync(AudioTranslationContext context, WhisperProcessor processor)
    {
        var wavBytes = PcmWavWriter.BuildWavBytes(context.OriginalAudio!, context.SampleRate);
        using var wavStream = new MemoryStream(wavBytes);

        var fragments = new List<AudioFragment>();
        await foreach (var segment in processor.ProcessAsync(wavStream))
        {
            var text = segment.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
                continue;
            if (text.StartsWith('[') && text.EndsWith(']'))
                continue;

            fragments.Add(new AudioFragment
            {
                OriginalText = text,
                StartMs = (long)segment.Start.TotalMilliseconds,
                EndMs = (long)segment.End.TotalMilliseconds,
                Confidence = 1.0f
            });
        }

        return fragments;
    }

    private static void ConfigureRuntimeLibraryOrder(WhisperGpuRuntime choice)
    {

        List<RuntimeLibrary>? order = choice switch
        {
            WhisperGpuRuntime.Vulkan => new List<RuntimeLibrary>
            {
                RuntimeLibrary.Vulkan,
                RuntimeLibrary.Cpu,
                RuntimeLibrary.CpuNoAvx
            },
            WhisperGpuRuntime.Cpu => new List<RuntimeLibrary>
            {
                RuntimeLibrary.Cpu,
                RuntimeLibrary.CpuNoAvx
            },
            _ => null
        };

        if (order is not null)
            RuntimeOptions.RuntimeLibraryOrder = order;
    }

    public void Dispose()
    {
        lock (_cacheLock)
        {
            _cachedProcessor?.Dispose();
            _cachedProcessor = null;
            _cachedProcessorLang = null;
            _cachedFactory?.Dispose();
            _cachedFactory = null;
            _cachedFactoryPath = null;
        }
    }
}
