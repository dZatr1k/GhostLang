using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class LanguageSelectionItem : ObservableObject
{
    public SupportedLanguage Language { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}