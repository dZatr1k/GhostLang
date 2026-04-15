using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Settings.Ocr;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class OcrSpaceViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty]
    private string _apiKey = "";

    [ObservableProperty]
    private int _selectedEngine = 1;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    public Dictionary<int, string> AvailableEngines { get; } = BuildEngines();

    private static Dictionary<int, string> BuildEngines()
    {
        var l = Services.LocalizationService.Instance;
        return new Dictionary<int, string>
        {
            { 1, l?["Engine_OcrSpaceEngine1"] ?? "Engine 1 — Fast, Asian languages" },
            { 2, l?["Engine_OcrSpaceEngine2"] ?? "Engine 2 — Auto-detect, Latin + Chinese" }
        };
    }

    public string EngineName => "OCR.space (Cloud)";
    public Type OptionsType => typeof(OcrSpaceOptions);

    public object GetOptions()
    {
        return new OcrSpaceOptions
        {
            ApiKey = ApiKey,
            OcrEngine = SelectedEngine
        };
    }

    public void ApplyOptions(object options)
    {
        if (options is not OcrSpaceOptions opt) return;

        ApiKey = opt.ApiKey;
        SelectedEngine = opt.OcrEngine;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestResult = Services.LocalizationService.Instance?["Engine_Testing"] ?? "Testing...";

        try
        {
            var engine = new OcrSpaceEngine(new OcrSpaceOptions
            {
                ApiKey = ApiKey,
                OcrEngine = SelectedEngine
            });

            var isSupported = await engine.IsLanguageSupportedAsync(SupportedLanguage.English);
            TestResult = isSupported
                ? Services.LocalizationService.Instance?["Engine_TestConnectionOk"] ?? "Connection OK"
                : Services.LocalizationService.Instance?["Engine_TestConnectionFail"] ?? "Failed";
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