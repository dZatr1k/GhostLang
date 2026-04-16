using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GhostLang.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly HomeViewModel _homeViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly DebugViewModel _debugViewModel;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    public bool HasUnsavedSettings => _settingsViewModel.HasUnsavedChanges;

    public MainViewModel(HomeViewModel homeViewModel,
        SettingsViewModel settingsViewModel,
        DebugViewModel debugViewModel)
    {
        _homeViewModel = homeViewModel;
        _settingsViewModel = settingsViewModel;
        _debugViewModel = debugViewModel;

        _currentViewModel = _homeViewModel;

        _homeViewModel.NavigateToSettingsRequested += NavigateSettings;

        _settingsViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.HasUnsavedChanges))
                OnPropertyChanged(nameof(HasUnsavedSettings));
        };
    }

    [RelayCommand]
    private void NavigateMain()
    {
        CurrentViewModel = _homeViewModel;
        _settingsViewModel.StopChangeTracking();
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        CurrentViewModel = _settingsViewModel;
        _settingsViewModel.StartChangeTracking();
    }

    [RelayCommand]
    private void NavigateDebug()
    {
        CurrentViewModel = _debugViewModel;
        _settingsViewModel.StopChangeTracking();
    }
}
