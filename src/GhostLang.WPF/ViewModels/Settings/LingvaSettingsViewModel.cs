using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class LingvaSettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty]
    private string _instanceUrl = "https://lingva.ml";

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    public string EngineName => "Lingva Translate (Self-hosted)";

    public Type OptionsType => typeof(LingvaOptions);

    public object GetOptions()
    {
        return new LingvaOptions
        {
            InstanceUrl = InstanceUrl
        };
    }

    public void ApplyOptions(object options)
    {
        if (options is not LingvaOptions opt) return;

        InstanceUrl = opt.InstanceUrl;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestResult = Services.LocalizationService.Instance?["Engine_Testing"] ?? "Testing...";

        try
        {
            var engine = new LingvaEngine(new LingvaOptions { InstanceUrl = InstanceUrl });
            var result = await engine.TranslateAsync("Hello", SupportedLanguage.Russian, [SupportedLanguage.English]);

            TestResult = result.StartsWith("[")
                ? result
                : string.Format(Services.LocalizationService.Instance?["Engine_TestOk"] ?? "OK: Hello → {0}", result);
        }
        catch (Exception ex)
        {
            TestResult = string.Format(Services.LocalizationService.Instance?["Engine_TestError"] ?? "Error: {0}", ex.Message);
        }
        finally
        {
            IsTesting = false;
        }
    }
}
