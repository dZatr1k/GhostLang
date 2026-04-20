using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GhostLang.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly HomeViewModel _homeViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly DebugViewModel _debugViewModel;
    private readonly BenchmarkViewModel _benchmarkViewModel;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    public MainViewModel(HomeViewModel homeViewModel,
        SettingsViewModel settingsViewModel,
        DebugViewModel debugViewModel,
        BenchmarkViewModel benchmarkViewModel)
    {
        _homeViewModel = homeViewModel;
        _settingsViewModel = settingsViewModel;
        _debugViewModel = debugViewModel;
        _benchmarkViewModel = benchmarkViewModel;

        _currentViewModel = _homeViewModel;

        _homeViewModel.NavigateToSettingsRequested += NavigateSettingsWithTab;
    }

    [RelayCommand]
    private void NavigateMain() => CurrentViewModel = _homeViewModel;

    [RelayCommand]
    private void NavigateSettings() => NavigateSettingsWithTab(null);

    private void NavigateSettingsWithTab(string? target)
    {
        CurrentViewModel = _settingsViewModel;
        if (target != null)
            _settingsViewModel.NavigateToTab(target);
    }

    [RelayCommand]
    private void NavigateDebug() => CurrentViewModel = _debugViewModel;

    [RelayCommand]
    private void NavigateBenchmark() => CurrentViewModel = _benchmarkViewModel;

    public bool IsAnyPipelineActive => _homeViewModel.IsAnyPipelineActive;

    public async Task ShutdownAsync()
    {
        _settingsViewModel.FlushPendingAutosave();
        await _homeViewModel.StopAllPipelinesAsync(TimeSpan.FromSeconds(3));
    }
}
