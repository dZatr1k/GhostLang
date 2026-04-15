using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Settings;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class GTranslateSettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty] 
    private GTranslateProvider _selectedProvider = GTranslateProvider.Google;

    public Dictionary<GTranslateProvider, string> AvailableProviders { get; } = BuildProviders();

    private static Dictionary<GTranslateProvider, string> BuildProviders()
    {
        var l = Services.LocalizationService.Instance;
        return new Dictionary<GTranslateProvider, string>
        {
            { GTranslateProvider.Google, l?["Engine_ProviderGoogle"] ?? "Google Translate" },
            { GTranslateProvider.Yandex, l?["Engine_ProviderYandex"] ?? "Yandex Translate" },
            { GTranslateProvider.Bing, l?["Engine_ProviderBing"] ?? "Bing Translator" },
            { GTranslateProvider.Microsoft, l?["Engine_ProviderMicrosoft"] ?? "Microsoft Translator" }
        };
    }
    
    public string EngineName => "GTranslate (Free)";

    public Type OptionsType => typeof(GTranslateOptions);

    public object GetOptions()
    {
        return new GTranslateOptions
        {
            Provider = SelectedProvider
        };
    }

    public void ApplyOptions(object options)
    {
        if (options is not GTranslateOptions opt) return;
            
        SelectedProvider = opt.Provider;
    }
}