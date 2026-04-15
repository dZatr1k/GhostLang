using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class LibreTranslateSettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty]
    private string _instanceUrl = "http://localhost:5000";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    public string EngineName => "LibreTranslate (Self-hosted, CTranslate2)";

    public Type OptionsType => typeof(LibreTranslateOptions);

    public object GetOptions()
    {
        return new LibreTranslateOptions
        {
            InstanceUrl = InstanceUrl,
            ApiKey = ApiKey
        };
    }

    public void ApplyOptions(object options)
    {
        if (options is not LibreTranslateOptions opt) return;
        
        InstanceUrl = opt.InstanceUrl;
        ApiKey = opt.ApiKey;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestResult = Services.LocalizationService.Instance?["Engine_Testing"] ?? "Testing...";

        try
        {
            var engine = new LibreTranslateEngine(new LibreTranslateOptions
            {
                InstanceUrl = InstanceUrl,
                ApiKey = ApiKey
            });
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