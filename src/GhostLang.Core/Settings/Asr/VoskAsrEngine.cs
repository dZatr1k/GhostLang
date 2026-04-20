using System.Text.Json;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;
using Vosk;

namespace GhostLang.Core.Settings.Asr;

public class VoskAsrEngine : IAsrEngine, IDisposable
{
    private readonly VoskAsrOptions _options;

    private readonly object _cacheLock = new();
    private Model? _cachedModel;
    private string? _cachedModelPath;
    private VoskRecognizer? _cachedRecognizer;
    private int _cachedSampleRate;

    public VoskAsrEngine(VoskAsrOptions options)
    {
        _options = options;
    }

    public IReadOnlySet<SupportedLanguage> SupportedLanguages => LanguageCapabilitySets.AllTwenty;

    public bool SupportsStreaming => true;

    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        return Task.FromResult(language != SupportedLanguage.Unknown);
    }

    public Task<List<AudioFragment>> RecognizeAsync(AudioTranslationContext context, List<SupportedLanguage> languages)
    {
        if (context.OriginalAudio is null || context.OriginalAudio.Length == 0)
            return Task.FromResult(new List<AudioFragment>());

        if (string.IsNullOrWhiteSpace(_options.ModelPath))
        {
            throw new InvalidOperationException(
                "Vosk model is not selected. Open Settings → Audio pipeline → ASR → Vosk, " +
                "unpack a model archive from https://alphacephei.com/vosk/models into the Models folder, " +
                "click Refresh and pick one from the list.");
        }

        if (!Directory.Exists(_options.ModelPath))
        {
            throw new DirectoryNotFoundException(
                $"Vosk model directory not found at '{_options.ModelPath}'. " +
                "Check the model was fully unpacked and the path is still correct in Settings.");
        }

        var requiredFiles = new[] { Path.Combine("am", "final.mdl"), Path.Combine("conf", "model.conf") };
        foreach (var rel in requiredFiles)
        {
            var full = Path.Combine(_options.ModelPath, rel);
            if (!File.Exists(full))
            {
                throw new InvalidOperationException(
                    $"Vosk model at '{_options.ModelPath}' is incomplete (missing {rel}). " +
                    "Re-extract the model archive — some tools skip hidden files during unzip.");
            }
        }

        Vosk.Vosk.SetLogLevel(-1);

        VoskRecognizer recognizer;
        try
        {
            recognizer = GetOrCreateRecognizer(context.SampleRate);
        }
        catch (Exception ex)
        {

            Dispose();
            throw new InvalidOperationException(
                $"Failed to load Vosk model from '{_options.ModelPath}': {ex.Message}. " +
                "Try re-downloading the model archive.",
                ex);
        }

        recognizer.SetWords(true);
        recognizer.SetMaxAlternatives(0);

        var subChunkSize = context.SampleRate;
        var pcm = context.OriginalAudio;

        for (var offset = 0; offset < pcm.Length; offset += subChunkSize)
        {
            var length = Math.Min(subChunkSize, pcm.Length - offset);
            var buffer = new byte[length];
            Array.Copy(pcm, offset, buffer, 0, length);
            recognizer.AcceptWaveform(buffer, length);
        }

        var resultJson = recognizer.FinalResult();
        var fragments = ParseVoskResult(resultJson);
        return Task.FromResult(fragments);
    }

    private VoskRecognizer GetOrCreateRecognizer(int sampleRate)
    {
        lock (_cacheLock)
        {

            var modelChanged = _cachedModel is null || _cachedModelPath != _options.ModelPath;
            var recognizerChanged = _cachedRecognizer is null || _cachedSampleRate != sampleRate || modelChanged;

            if (modelChanged)
            {
                _cachedRecognizer?.Dispose();
                _cachedRecognizer = null;
                _cachedModel?.Dispose();
                _cachedModel = new Model(_options.ModelPath);
                _cachedModelPath = _options.ModelPath;
            }

            if (recognizerChanged)
            {
                _cachedRecognizer?.Dispose();
                _cachedRecognizer = new VoskRecognizer(_cachedModel!, sampleRate);
                _cachedSampleRate = sampleRate;
            }
            else
            {

                _cachedRecognizer!.Reset();
            }

            return _cachedRecognizer!;
        }
    }

    private static List<AudioFragment> ParseVoskResult(string json)
    {
        var fragments = new List<AudioFragment>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("text", out var textProp))
            return fragments;

        var text = textProp.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return fragments;

        long startMs = 0;
        long endMs = 0;
        float conf = 1.0f;

        if (root.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
        {
            var words = resultProp.EnumerateArray().ToList();
            if (words.Count > 0)
            {
                if (words[0].TryGetProperty("start", out var startP))
                    startMs = (long)(startP.GetDouble() * 1000);
                if (words[^1].TryGetProperty("end", out var endP))
                    endMs = (long)(endP.GetDouble() * 1000);

                var confidences = words
                    .Where(w => w.TryGetProperty("conf", out _))
                    .Select(w => (float)w.GetProperty("conf").GetDouble())
                    .ToList();
                if (confidences.Count > 0)
                    conf = confidences.Average();
            }
        }

        fragments.Add(new AudioFragment
        {
            OriginalText = text,
            StartMs = startMs,
            EndMs = endMs,
            Confidence = conf
        });

        return fragments;
    }

    public void Dispose()
    {
        lock (_cacheLock)
        {
            _cachedRecognizer?.Dispose();
            _cachedRecognizer = null;
            _cachedModel?.Dispose();
            _cachedModel = null;
            _cachedModelPath = null;
        }
    }
}
