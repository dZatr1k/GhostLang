using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Utilities;
using GTranslate.Translators;

namespace GhostLang.Core.Settings.Translation;

public class GTranslateEngine(GTranslateOptions options) : ITranslationEngine
{
    private readonly ITranslator _translator = options.Provider switch
    {
        GTranslateProvider.Google => new GoogleTranslator(),
        GTranslateProvider.Yandex => new YandexTranslator(),
        GTranslateProvider.Bing => new BingTranslator(),
        GTranslateProvider.Microsoft => new MicrosoftTranslator(),
        _ => new GoogleTranslator()
    };

    public IReadOnlySet<SupportedLanguage> SupportedLanguages => LanguageCapabilitySets.AllTwenty;

    public async Task<string> TranslateAsync(string text, SupportedLanguage targetLanguage,
        List<SupportedLanguage> sourceLanguages)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var targetCode = targetLanguage.ToIsoLanguageCode();

        var primarySourceLang = SupportedLanguage.Unknown;
        if (sourceLanguages is { Count: 1 })
        {
            primarySourceLang = sourceLanguages.First();
        }

        try
        {
            if (primarySourceLang == SupportedLanguage.Unknown)
            {
                var result = await _translator.TranslateAsync(text, targetCode);
                return result.Translation;
            }
            else
            {
                var sourceCode = primarySourceLang.ToIsoLanguageCode();
                var result = await _translator.TranslateAsync(text, targetCode, sourceCode);
                return result.Translation;
            }
        }
        catch (Exception ex)
        {
            return $"[Translation error ({_translator.GetType().Name}): {ex.Message}]";
        }
    }
}
