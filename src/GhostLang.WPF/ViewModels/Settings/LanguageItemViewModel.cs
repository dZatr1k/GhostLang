using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class LanguageItemViewModel(SupportedLanguage language, string displayName) : ObservableObject
{
    public SupportedLanguage Language { get; } = language;
    public string DisplayName { get; } = displayName;

    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private string _statusText = string.Empty;
    
    [ObservableProperty] 
    private bool _isSelected;
}