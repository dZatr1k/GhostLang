using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Settings.Ocr;

public interface IOcrEngine
{

    IReadOnlySet<SupportedLanguage> SupportedLanguages { get; }

    Task<bool> IsLanguageSupportedAsync(SupportedLanguage language);

    Task<List<TextFragment>> RecognizeTextAsync(TranslationContext context, List<SupportedLanguage> language);
}
