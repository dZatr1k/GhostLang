using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class PipelineStepViewModel : ObservableObject
{
    [ObservableProperty] private string _stepId = string.Empty;

    [ObservableProperty] private string _stepName = string.Empty;

    [ObservableProperty] private string _description = string.Empty;

    [ObservableProperty] private bool _isOptional;

    [ObservableProperty] private bool _isEnabled = true;

    public ObservableCollection<IEngineSettingsViewModel> AvailableEngines { get; } = new();

    [ObservableProperty] private IEngineSettingsViewModel? _selectedEngineViewModel;

    [ObservableProperty] private int _stepNumber;
}