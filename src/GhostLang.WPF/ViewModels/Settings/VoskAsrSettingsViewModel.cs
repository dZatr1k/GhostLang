using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Services.Asr;
using GhostLang.Core.Settings.Asr;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class VoskAsrSettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    private readonly IVoskModelManager _modelManager;

    [ObservableProperty] private string _modelsRootPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Vosk");

    [ObservableProperty] private string _modelPath = string.Empty;

    public ObservableCollection<VoskModelViewModel> DetectedModels { get; } = new();

    [ObservableProperty] private string _detectionSummary = string.Empty;

    public VoskAsrSettingsViewModel(IVoskModelManager modelManager)
    {
        _modelManager = modelManager;
        RefreshModels();
    }

    partial void OnModelsRootPathChanged(string value) => RefreshModels();

    [RelayCommand]
    private void BrowseRoot()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog();
        if (dialog.ShowDialog() == true)
            ModelsRootPath = dialog.FolderName;
    }

    [RelayCommand]
    private void OpenRootFolder()
    {
        try
        {
            Directory.CreateDirectory(ModelsRootPath);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ModelsRootPath,
                UseShellExecute = true
            });
        }
        catch
        {

        }
    }

    [RelayCommand]
    private void RefreshModels()
    {
        DetectedModels.Clear();

        var models = _modelManager.DiscoverModels(ModelsRootPath);
        foreach (var m in models)
            DetectedModels.Add(new VoskModelViewModel(m, IsSelected: m.FullPath == ModelPath));

        var loc = Services.LocalizationService.Instance;
        if (!Directory.Exists(ModelsRootPath))
        {
            DetectionSummary = loc?["Engine_Vosk_RootMissing"] ?? "Root folder does not exist yet.";
        }
        else if (models.Count == 0)
        {
            DetectionSummary = loc?["Engine_Vosk_NoModels"] ?? "No models found in the root folder.";
        }
        else
        {
            var valid = models.Count(m => m.IsValid);
            var template = loc?["Engine_Vosk_DetectedSummary"] ?? "Detected: {0} valid / {1} total.";
            DetectionSummary = string.Format(template, valid, models.Count);
        }
    }

    [RelayCommand]
    private void SelectModel(VoskModelViewModel? model)
    {
        if (model is null || !model.Info.IsValid) return;

        ModelPath = model.Info.FullPath;
        foreach (var m in DetectedModels)
            m.IsSelected = m.Info.FullPath == ModelPath;
    }

    [RelayCommand]
    private void OpenDownloadPage()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://alphacephei.com/vosk/models",
            UseShellExecute = true
        });
    }

    public string EngineName => "Vosk (Local)";
    public Type OptionsType => typeof(VoskAsrOptions);

    public object GetOptions() => new VoskAsrOptions
    {
        ModelsRootPath = ModelsRootPath,
        ModelPath = ModelPath
    };

    public void ApplyOptions(object options)
    {
        if (options is not VoskAsrOptions opt) return;
        if (!string.IsNullOrWhiteSpace(opt.ModelsRootPath))
            ModelsRootPath = opt.ModelsRootPath;
        ModelPath = opt.ModelPath ?? string.Empty;
        RefreshModels();
    }
}

public partial class VoskModelViewModel : ObservableObject
{
    public VoskModelInfo Info { get; }
    public string Name => Info.Name;
    public string FullPath => Info.FullPath;
    public bool IsValid => Info.IsValid;
    public string? InvalidReason => Info.InvalidReason;

    [ObservableProperty] private bool _isSelected;

    public VoskModelViewModel(VoskModelInfo info, bool IsSelected)
    {
        Info = info;
        _isSelected = IsSelected;
    }
}
