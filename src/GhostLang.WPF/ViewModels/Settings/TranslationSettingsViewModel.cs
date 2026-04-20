using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class TranslationSettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty]
    private TranslationEngineType _selectedEngine = TranslationEngineType.GoogleWeb;

    public Dictionary<TranslationEngineType, string> AvailableEngines { get; } = new()
    {
        { TranslationEngineType.GoogleWeb, "Google Web Translator (Free, no API)" },
        { TranslationEngineType.DeepL, "DeepL Pro API (Key required)" },
        { TranslationEngineType.MyMemory, "MyMemory API (Limited)" }
    };

    public string EngineName => "Google Web Translator";

    public Type OptionsType => typeof(TranslationEngineOptions);

    public object GetOptions()
    {
        return SelectedEngine switch
        {
            TranslationEngineType.GoogleWeb => new GTranslateOptions(),
            _ => new GTranslateOptions()
        };
    }

    public void ApplyOptions(object options)
    {
        if (options is GTranslateOptions)
            SelectedEngine = TranslationEngineType.GoogleWeb;
    }
}
