using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Settings.Translation;

public interface ITranslationEngine
{

    IReadOnlySet<SupportedLanguage> SupportedLanguages { get; }

    Task<string> TranslateAsync(string text, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguages);
}
