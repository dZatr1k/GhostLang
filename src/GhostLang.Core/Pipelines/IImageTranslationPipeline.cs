using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Pipelines;

public interface IImageTranslationPipeline
{
    Task<TranslationContext> ProcessFrameAsync(byte[] imageBytes, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguage);
}