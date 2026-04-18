using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;
using GhostLang.Core.Services.Asr;
using GhostLang.Core.Services.AudioCapture;
using Whisper.net;

namespace GhostLang.Core.Settings.Asr;

public class WhisperAsrEngine : IAsrEngine
{
    private readonly WhisperAsrOptions _options;
    private readonly IWhisperModelManager _modelManager;

    public WhisperAsrEngine(WhisperAsrOptions options, IWhisperModelManager modelManager)
    {
        _options = options;
        _modelManager = modelManager;
    }

    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        return Task.FromResult(language != SupportedLanguage.Unknown);
    }

    public async Task<List<AudioFragment>> RecognizeAsync(AudioTranslationContext context, List<SupportedLanguage> languages)
    {
        if (context.OriginalAudio is null || context.OriginalAudio.Length == 0)
            return new List<AudioFragment>();

        var modelPath = await _modelManager.EnsureModelAsync(_options.ModelName, _options.ModelsPath);

        using var factory = WhisperFactory.FromPath(modelPath);

        var sourceLang = languages.FirstOrDefault(l => l != SupportedLanguage.Unknown);
        var langCode = sourceLang != SupportedLanguage.Unknown
            ? sourceLang.ToIsoLanguageCode()
            : "auto";

        using var processor = factory.CreateBuilder()
            .WithLanguage(langCode)
            .Build();

        var wavBytes = PcmWavWriter.BuildWavBytes(context.OriginalAudio, context.SampleRate);
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
}