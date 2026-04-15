using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services.Ocr;
using GhostLang.Core.Settings.Ocr;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class TesseractOcrViewModel : ObservableObject, IEngineSettingsViewModel
{
    private readonly ITesseractModelManager _modelManager;

    [ObservableProperty] private TesseractModelType _modelType = TesseractModelType.Fast;

    public ObservableCollection<LanguageItemViewModel> Languages { get; } = new();

    public Dictionary<TesseractModelType, string> AvailableModelTypes { get; } = BuildModelTypes();

    private static Dictionary<TesseractModelType, string> BuildModelTypes()
    {
        var l = Services.LocalizationService.Instance;
        return new Dictionary<TesseractModelType, string>
        {
            { TesseractModelType.Fast, l?["Engine_ModelFast"] ?? "Fast — lighter, faster" },
            { TesseractModelType.Best, l?["Engine_ModelBest"] ?? "Best — most accurate, slower" }
        };
    }

    public ObservableCollection<LanguageSelectionItem> AvailableLanguages { get; } = new();
    
    public TesseractOcrViewModel(ITesseractModelManager modelManager)
    {
        _modelManager = modelManager;
        InitializeLanguages();
        CheckAllModelsStatus();
    }

    partial void OnModelTypeChanged(TesseractModelType value) => CheckAllModelsStatus();

    private void InitializeLanguages()
    {
        Languages.Clear();
        var availableLangs = Enum.GetValues(typeof(SupportedLanguage))
            .Cast<SupportedLanguage>()
            .Where(l => l != SupportedLanguage.Unknown);

        foreach (var lang in availableLangs)
        {
            Languages.Add(new LanguageItemViewModel(lang, lang.ToString()));
        }
    }

    private void CheckAllModelsStatus()
    {
        foreach (var item in Languages)
        {
            item.IsDownloaded = _modelManager.IsModelDownloaded(item.Language, ModelType);
            var loc = Services.LocalizationService.Instance;
            item.StatusText = item.IsDownloaded ? (loc?["Engine_Downloaded"] ?? "Downloaded") : (loc?["Engine_Download"] ?? "Download");
            item.DownloadProgress = 0;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task DownloadModelAsync(LanguageItemViewModel item)
    {
        if (item == null || item.IsDownloading) return;
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        item.IsDownloading = true;
        item.StatusText = Services.LocalizationService.Instance?["Engine_Downloading"] ?? "Downloading...";
        item.DownloadProgress = 0;

        try
        {
            var progress = new Progress<double>(value => item.DownloadProgress = value);
            await _modelManager.DownloadModelAsync(item.Language, ModelType, progress, ct);

            item.IsDownloaded = true;
            item.StatusText = Services.LocalizationService.Instance?["Engine_Downloaded"] ?? "Downloaded";
        }
        catch (OperationCanceledException)
        {
            item.StatusText = Services.LocalizationService.Instance?["Misc_Cancelled"] ?? "Cancelled";
        }
        catch (Exception ex)
        {
            item.StatusText = $"{Services.LocalizationService.Instance?["Misc_Error"]} {ex.Message}";
        }
        finally
        {
            item.IsDownloading = false;
        }
    }

    public void ApplyOptions(object options)
    {
        if (options is TesseractOcrOptions opt)
        {
            ModelType = opt.ModelType;
            CheckAllModelsStatus();
            
            foreach (var item in Languages) item.IsSelected = false;

            foreach (var lang in opt.SourceLanguages)
            {
                var match = Languages.FirstOrDefault(x => x.Language == lang);
                if (match != null) match.IsSelected = true;
            }
        }
    }

    public string EngineName => "TesseractOcr";
    public Type OptionsType => typeof(TesseractOcrOptions);

    public object GetOptions()
    {
        var selectedLangs = Languages
            .Where(x => x.IsSelected)
            .Select(x => x.Language)
            .ToList();

        if (selectedLangs.Count == 0)
        {
            selectedLangs.Add(SupportedLanguage.English);
        }
        return new TesseractOcrOptions
        {
            ModelType = ModelType,
            SourceLanguages = selectedLangs
        };
    }
}