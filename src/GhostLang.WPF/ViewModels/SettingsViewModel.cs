using System.Windows;

namespace GhostLang.WPF.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private int _timerIntervalMilliseconds = 50;
    private Rect _selectedArea;

    public int TimerIntervalMilliseconds
    {
        get => _timerIntervalMilliseconds;
        set => SetField(ref _timerIntervalMilliseconds, value);
    }
    
    public Rect SelectedArea
    {
        get => _selectedArea;
        set => SetField(ref _selectedArea, value);
    }
}