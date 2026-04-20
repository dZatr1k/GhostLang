using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Pipelines.Utilities;

public static class LanguageCapabilitySets
{

    public static IReadOnlySet<SupportedLanguage> AllTwenty { get; } = BuildAllTwenty();

    public static IReadOnlySet<SupportedLanguage> AzureVisionCore { get; } = new HashSet<SupportedLanguage>
    {
        SupportedLanguage.English,
        SupportedLanguage.Russian,
        SupportedLanguage.Japanese,
        SupportedLanguage.ChineseSimplified,
        SupportedLanguage.French,
        SupportedLanguage.German,
        SupportedLanguage.Spanish
    };

    public static IReadOnlySet<SupportedLanguage> OcrSpace { get; } = new HashSet<SupportedLanguage>
    {
        SupportedLanguage.English,
        SupportedLanguage.Russian,
        SupportedLanguage.Japanese,
        SupportedLanguage.ChineseSimplified,
        SupportedLanguage.ChineseTraditional,
        SupportedLanguage.French,
        SupportedLanguage.German,
        SupportedLanguage.Spanish,
        SupportedLanguage.Italian,
        SupportedLanguage.Portuguese,
        SupportedLanguage.Polish,
        SupportedLanguage.Korean,
        SupportedLanguage.Arabic,
        SupportedLanguage.Turkish,
        SupportedLanguage.Ukrainian,
        SupportedLanguage.Dutch,
        SupportedLanguage.Vietnamese,
        SupportedLanguage.Thai
    };

    private static IReadOnlySet<SupportedLanguage> BuildAllTwenty()
    {
        var set = new HashSet<SupportedLanguage>();
        foreach (SupportedLanguage lang in Enum.GetValues<SupportedLanguage>())
        {
            if (lang != SupportedLanguage.Unknown)
                set.Add(lang);
        }
        return set;
    }
}
