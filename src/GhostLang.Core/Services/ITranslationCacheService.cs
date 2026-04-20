using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Services;

public interface ITranslationCacheService
{
    bool TryGetTranslation(string originalText, SupportedLanguage targetLanguage, out string? translatedText);
    void AddTranslation(string originalText, string translatedText, SupportedLanguage targetLanguage);
    void SetEngineTag(string engineTag);
    void Configure(int ttlMinutes, int maxCharacters);
    void ClearCache();
}
