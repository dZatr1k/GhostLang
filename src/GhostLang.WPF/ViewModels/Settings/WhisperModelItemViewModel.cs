using CommunityToolkit.Mvvm.ComponentModel;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class WhisperModelItemViewModel : ObservableObject
{
    [ObservableProperty] private string _modelName = string.Empty;

    [ObservableProperty] private string _sizeInfo = string.Empty;

    [ObservableProperty] private bool _isDownloaded;

    [ObservableProperty] private bool _isDownloading;

    [ObservableProperty] private string _statusText = string.Empty;
}