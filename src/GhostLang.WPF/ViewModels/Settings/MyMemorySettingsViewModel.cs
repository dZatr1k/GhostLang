using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Settings;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class MyMemorySettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    public string EngineName => "MyMemory API (Translated.net)";

    public Type OptionsType => typeof(MyMemoryOptions);

    public object GetOptions()
    {
        return new MyMemoryOptions
        {
            Email = Email,
            ApiKey = ApiKey
        };
    }

    public void ApplyOptions(object options)
    {
        if (options is not MyMemoryOptions opt) return;

        Email = opt.Email;
        ApiKey = opt.ApiKey;
    }
}