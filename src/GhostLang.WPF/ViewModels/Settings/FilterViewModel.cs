using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Settings.ImagePreProcessing;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class FilterViewModel : ObservableObject
{
    [ObservableProperty] private string _displayName = string.Empty;

    [ObservableProperty] private string _description = string.Empty;

    [ObservableProperty] private bool _hasParameter = true;

    public FilterOption Option { get; set; } = null!;

    public bool IsEnabled
    {
        get => Option?.IsEnabled ?? false;
        set
        {
            if (Option == null || Option.IsEnabled == value) return;
            Option.IsEnabled = value;
            OnPropertyChanged();
        }
    }

    public float Value
    {
        get => Option?.Value ?? 0f;
        set
        {
            if (Option == null || Math.Abs(Option.Value - value) < float.Epsilon) return;
            Option.Value = value;
            OnPropertyChanged();
        }
    }
}
