using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Settings.Ocr;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class AzureVisionOcrViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty]
    private string _endpointUrl = "";

    [ObservableProperty]
    private string _apiKey = "";

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    public string EngineName => "Azure AI Vision";
    public Type OptionsType => typeof(AzureVisionOcrOptions);

    public object GetOptions()
    {
        return new AzureVisionOcrOptions
        {
            EndpointUrl = EndpointUrl,
            ApiKey = ApiKey
        };
    }

    public void ApplyOptions(object options)
    {
        if (options is not AzureVisionOcrOptions opt) return;

        EndpointUrl = opt.EndpointUrl;
        ApiKey = opt.ApiKey;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestResult = Services.LocalizationService.Instance?["Engine_Testing"] ?? "Testing...";

        try
        {
            var engine = new AzureVisionOcrEngine(new AzureVisionOcrOptions
            {
                EndpointUrl = EndpointUrl,
                ApiKey = ApiKey
            });

            var isSupported = await engine.IsLanguageSupportedAsync(SupportedLanguage.English);
            TestResult = isSupported
                ? Services.LocalizationService.Instance?["Engine_TestConnectionOk"] ?? "Connection OK"
                : Services.LocalizationService.Instance?["Engine_TestConnectionFail"] ?? "Connection failed";
        }
        catch (Exception ex)
        {
            TestResult = string.Format(
                Services.LocalizationService.Instance?["Engine_TestError"] ?? "Error: {0}", ex.Message);
        }
        finally
        {
            IsTesting = false;
        }
    }
}
