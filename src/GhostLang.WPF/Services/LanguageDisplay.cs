using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.WPF.Services;

public static class LanguageDisplay
{
    public static string ToDisplayName(this SupportedLanguage language)
    {
        var key = language switch
        {
            SupportedLanguage.English => "Language_English",
            SupportedLanguage.Russian => "Language_Russian",
            SupportedLanguage.Spanish => "Language_Spanish",
            SupportedLanguage.German => "Language_German",
            SupportedLanguage.French => "Language_French",
            SupportedLanguage.Japanese => "Language_Japanese",
            SupportedLanguage.ChineseSimplified => "Language_ChineseSimplified",
            SupportedLanguage.ChineseTraditional => "Language_ChineseTraditional",
            SupportedLanguage.Italian => "Language_Italian",
            SupportedLanguage.Portuguese => "Language_Portuguese",
            SupportedLanguage.Polish => "Language_Polish",
            SupportedLanguage.Korean => "Language_Korean",
            SupportedLanguage.Arabic => "Language_Arabic",
            SupportedLanguage.Turkish => "Language_Turkish",
            SupportedLanguage.Ukrainian => "Language_Ukrainian",
            SupportedLanguage.Dutch => "Language_Dutch",
            SupportedLanguage.Vietnamese => "Language_Vietnamese",
            SupportedLanguage.Hindi => "Language_Hindi",
            SupportedLanguage.Thai => "Language_Thai",
            SupportedLanguage.Hebrew => "Language_Hebrew",
            _ => null
        };

        if (key is null) return language.ToString();
        return LocalizationService.Instance?[key] ?? language.ToString();
    }
}
