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
            SupportedLanguage.ChineseSimplified => "zh-CN",
            SupportedLanguage.ChineseTraditional => "zh-TW",
            SupportedLanguage.French => "fr",
            SupportedLanguage.German => "de",
            SupportedLanguage.Spanish => "es",
            SupportedLanguage.Italian => "it",
            SupportedLanguage.Portuguese => "pt",
            SupportedLanguage.Polish => "pl",
            SupportedLanguage.Korean => "ko",
            SupportedLanguage.Arabic => "ar",
            SupportedLanguage.Turkish => "tr",
            SupportedLanguage.Ukrainian => "uk",
            SupportedLanguage.Dutch => "nl",
            SupportedLanguage.Vietnamese => "vi",
            SupportedLanguage.Hindi => "hi",
            SupportedLanguage.Thai => "th",
            SupportedLanguage.Hebrew => "he",
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
            SupportedLanguage.ChineseSimplified => "zh-Hans-CN",
            SupportedLanguage.ChineseTraditional => "zh-Hant-TW",
            SupportedLanguage.French => "fr-FR",
            SupportedLanguage.German => "de-DE",
            SupportedLanguage.Spanish => "es-ES",
            SupportedLanguage.Italian => "it-IT",
            SupportedLanguage.Portuguese => "pt-PT",
            SupportedLanguage.Polish => "pl-PL",
            SupportedLanguage.Korean => "ko-KR",
            SupportedLanguage.Arabic => "ar-SA",
            SupportedLanguage.Turkish => "tr-TR",
            SupportedLanguage.Ukrainian => "uk-UA",
            SupportedLanguage.Dutch => "nl-NL",
            SupportedLanguage.Vietnamese => "vi-VN",
            SupportedLanguage.Hindi => "hi-IN",
            SupportedLanguage.Thai => "th-TH",
            SupportedLanguage.Hebrew => "he-IL",
            _ => "en-US"
        };
    }

    public static string ToTesseractCode(this SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.Russian => "rus",
            SupportedLanguage.English => "eng",
            SupportedLanguage.ChineseSimplified => "chi_sim",
            SupportedLanguage.ChineseTraditional => "chi_tra",
            SupportedLanguage.French => "fra",
            SupportedLanguage.German => "deu",
            SupportedLanguage.Japanese => "jpn",
            SupportedLanguage.Spanish => "spa",
            SupportedLanguage.Italian => "ita",
            SupportedLanguage.Portuguese => "por",
            SupportedLanguage.Polish => "pol",
            SupportedLanguage.Korean => "kor",
            SupportedLanguage.Arabic => "ara",
            SupportedLanguage.Turkish => "tur",
            SupportedLanguage.Ukrainian => "ukr",
            SupportedLanguage.Dutch => "nld",
            SupportedLanguage.Vietnamese => "vie",
            SupportedLanguage.Hindi => "hin",
            SupportedLanguage.Thai => "tha",
            SupportedLanguage.Hebrew => "heb",
            _ => throw new NotSupportedException("Language not supported by Tesseract.")
        };
    }

    public static string ToWhisperLanguageCode(this SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.English => "en",
            SupportedLanguage.Russian => "ru",
            SupportedLanguage.Spanish => "es",
            SupportedLanguage.German => "de",
            SupportedLanguage.French => "fr",
            SupportedLanguage.Japanese => "ja",
            SupportedLanguage.ChineseSimplified => "zh",
            SupportedLanguage.ChineseTraditional => "zh",
            SupportedLanguage.Italian => "it",
            SupportedLanguage.Portuguese => "pt",
            SupportedLanguage.Polish => "pl",
            SupportedLanguage.Korean => "ko",
            SupportedLanguage.Arabic => "ar",
            SupportedLanguage.Turkish => "tr",
            SupportedLanguage.Ukrainian => "uk",
            SupportedLanguage.Dutch => "nl",
            SupportedLanguage.Vietnamese => "vi",
            SupportedLanguage.Hindi => "hi",
            SupportedLanguage.Thai => "th",
            SupportedLanguage.Hebrew => "he",
            _ => "auto"
        };
    }
}
