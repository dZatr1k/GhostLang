using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Settings.Asr;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class AzureAsrSettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty] private string _apiKey = string.Empty;

    [ObservableProperty] private string _region = "westeurope";

    public string EngineName => "Azure AI Speech (Cloud)";
    public Type OptionsType => typeof(AzureAsrOptions);

    public object GetOptions() => new AzureAsrOptions
    {
        ApiKey = ApiKey,
        Region = Region
    };

    public void ApplyOptions(object options)
    {
        if (options is not AzureAsrOptions opt) return;
        ApiKey = opt.ApiKey;
        Region = opt.Region;
    }
}