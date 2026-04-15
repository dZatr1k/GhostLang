using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Pipelines.Utilities;

public static class LanguageMapper
{
    public static string ToIsoLanguageCode(this SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.Russian => "ru",
            SupportedLanguage.English => "en",
            SupportedLanguage.Japanese => "ja",
            SupportedLanguage.Chinese => "zh-CN",
            SupportedLanguage.French => "fr",
            SupportedLanguage.German => "de",
            SupportedLanguage.Spanish => "es",
            _ => "en"
        };
    }
    public static string ToWindowsLanguageTag(this SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.Russian => "ru-RU",
            SupportedLanguage.English => "en-US",
            SupportedLanguage.Japanese => "ja-JP",
            SupportedLanguage.Chinese => "zh-Hans-CN",
            SupportedLanguage.French => "fr-FR",
            SupportedLanguage.German => "de-DE",
            SupportedLanguage.Spanish => "es-ES",
            _ => "en-US"
        };
    }

    public static string ToTesseractCode(this SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.Russian => "rus",
            SupportedLanguage.English => "eng",
            SupportedLanguage.Chinese => "chi_sim",
            SupportedLanguage.French => "fra",
            SupportedLanguage.German => "deu",
            SupportedLanguage.Japanese => "jpn",
            SupportedLanguage.Spanish => "spa",
            _ => throw new NotSupportedException("Language not supported by Tesseract.")
        };
    }
}