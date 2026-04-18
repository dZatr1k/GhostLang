using System.Text.Json;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using Vosk;

namespace GhostLang.Core.Settings.Asr;

public class VoskAsrEngine : IAsrEngine
{
    private readonly VoskAsrOptions _options;

    public VoskAsrEngine(VoskAsrOptions options)
    {
        _options = options;
    }

    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        return Task.FromResult(language != SupportedLanguage.Unknown);
    }

    public Task<List<AudioFragment>> RecognizeAsync(AudioTranslationContext context, List<SupportedLanguage> languages)
    {
        if (context.OriginalAudio is null || context.OriginalAudio.Length == 0)
            return Task.FromResult(new List<AudioFragment>());

        if (!Directory.Exists(_options.ModelPath))
        {
            throw new DirectoryNotFoundException(
                $"Vosk model directory not found at '{_options.ModelPath}'. " +
                "Download a Vosk model from https://alphacephei.com/vosk/models (e.g. vosk-model-small-ru-0.22), " +
                "extract the archive and point VoskAsrOptions.ModelPath to the extracted folder.");
        }

        Vosk.Vosk.SetLogLevel(0);

        var model = new Model(_options.ModelPath);
        var recognizer = new VoskRecognizer(model, context.SampleRate);
        try
        {
            recognizer.SetWords(true);
            recognizer.SetMaxAlternatives(0);

            const int chunkSize = 8000;
            var pcm = context.OriginalAudio;

            for (int offset = 0; offset < pcm.Length; offset += chunkSize)
            {
                int length = Math.Min(chunkSize, pcm.Length - offset);
                var buffer = new byte[length];
                Array.Copy(pcm, offset, buffer, 0, length);
                recognizer.AcceptWaveform(buffer, length);
            }

            var resultJson = recognizer.FinalResult();
            var fragments = ParseVoskResult(resultJson);
            return Task.FromResult(fragments);
        }
        finally
        {
            recognizer.Dispose();
            model.Dispose();
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
}