using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Pipelines;

public interface IAudioTranslationPipeline
{
    Task<AudioTranslationContext> ProcessAsync(
        byte[] audioPcm,
        int sampleRate,
        int channelCount,
        SupportedLanguage targetLanguage,
        List<SupportedLanguage> sourceLanguage,
        CancellationToken ct = default);
}