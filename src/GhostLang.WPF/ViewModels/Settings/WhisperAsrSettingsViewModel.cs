using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Services.Asr;
using GhostLang.Core.Settings.Asr;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class WhisperAsrSettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    private readonly IWhisperModelManager _modelManager;

    [ObservableProperty] private string _modelsPath = "Models/Whisper";

    [ObservableProperty] private WhisperModelItemViewModel? _selectedModel;

    public ObservableCollection<WhisperModelItemViewModel> Models { get; } = new();

    private static readonly string[] KnownModels = { "ggml-tiny", "ggml-base", "ggml-small", "ggml-medium", "ggml-large-v3" };

    public WhisperAsrSettingsViewModel(IWhisperModelManager modelManager)
    {
        _modelManager = modelManager;
        InitializeModels();
    }

    private void InitializeModels()
    {
        Models.Clear();
        foreach (var name in KnownModels)
        {
            var isDownloaded = _modelManager.IsModelDownloaded(name, ModelsPath);
            Models.Add(new WhisperModelItemViewModel
            {
                ModelName = name,
                SizeInfo = GetModelSizeInfo(name),
                IsDownloaded = isDownloaded,
                StatusText = isDownloaded
                    ? Services.LocalizationService.Instance?["Engine_Downloaded"] ?? "Downloaded"
                    : Services.LocalizationService.Instance?["Engine_NotDownloaded"] ?? "Not downloaded"
            });
        }

        SelectedModel ??= Models.FirstOrDefault(m => m.ModelName == "ggml-base");
    }

    private static string GetModelSizeInfo(string name) => name switch
    {
        "ggml-tiny" => "~75 MB",
        "ggml-base" => "~142 MB",
        "ggml-small" => "~466 MB",
        "ggml-medium" => "~1.5 GB",
        "ggml-large-v3" => "~3 GB",
        _ => ""
    };

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task DownloadModelAsync(WhisperModelItemViewModel? item)
    {
        if (item is null || item.IsDownloading || item.IsDownloaded)
            return;

        item.IsDownloading = true;
        item.StatusText = Services.LocalizationService.Instance?["Engine_Downloading"] ?? "Downloading...";

        try
        {
            await _modelManager.EnsureModelAsync(item.ModelName, ModelsPath);
            item.IsDownloaded = true;
            item.StatusText = Services.LocalizationService.Instance?["Engine_Downloaded"] ?? "Downloaded";
        }
        catch (Exception ex)
        {
            item.StatusText = $"{Services.LocalizationService.Instance?["Misc_Error"] ?? "Error:"} {ex.Message}";
        }
        finally
        {
            item.IsDownloading = false;
        }
    }

    [RelayCommand]
    private void DeleteModel(WhisperModelItemViewModel? item)
    {
        if (item is null || !item.IsDownloaded)
            return;

        try
        {
            _modelManager.DeleteModel(item.ModelName, ModelsPath);
            item.IsDownloaded = false;
            item.StatusText = Services.LocalizationService.Instance?["Engine_NotDownloaded"] ?? "Not downloaded";
        }
        catch (Exception ex)
        {
            item.StatusText = $"{Services.LocalizationService.Instance?["Misc_Error"] ?? "Error:"} {ex.Message}";
        }
    }

    public string EngineName => "Whisper.net (Local)";
    public Type OptionsType => typeof(WhisperAsrOptions);

    public object GetOptions() => new WhisperAsrOptions
    {
        ModelName = SelectedModel?.ModelName ?? "ggml-base",
        ModelsPath = ModelsPath
    };

    public void ApplyOptions(object options)
    {
        if (options is not WhisperAsrOptions opt) return;

        ModelsPath = opt.ModelsPath;
        InitializeModels();
        SelectedModel = Models.FirstOrDefault(m => m.ModelName == opt.ModelName) ?? Models.FirstOrDefault();
    }
}