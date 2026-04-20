using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Pipelines;

public interface IAudioTranslationPipeline : IDisposable
{
    Task<AudioTranslationContext> ProcessAsync(
        byte[] audioPcm,
        int sampleRate,
        int channelCount,
        SupportedLanguage targetLanguage,
        List<SupportedLanguage> sourceLanguage,
        long? captureStartMs = null,
        CancellationToken ct = default);
}
