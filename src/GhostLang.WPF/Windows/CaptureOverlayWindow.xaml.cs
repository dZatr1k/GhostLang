using System.Windows;
using System.Windows.Input;
using GhostLang.WPF.ViewModels;

namespace GhostLang.WPF.Windows;

public partial class CaptureOverlayWindow
{
    private readonly SettingsViewModel _settingsViewModel;
    private const double ToolbarHeight = 30;

    public CaptureOverlayWindow(SettingsViewModel settingsViewModel)
    {
        _settingsViewModel = settingsViewModel;
        InitializeComponent();
        
        LocationChanged += OnLocationChanged;
    }
    private void Toolbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
    
    private void OnLocationChanged(object? sender, EventArgs e)
    {
        _settingsViewModel.SelectedArea = new Rect(
            Left,
            Top + ToolbarHeight,
            Width,
            Height - ToolbarHeight
        );
    }

    protected override void OnClosed(EventArgs e)
    {
        LocationChanged -= OnLocationChanged;
        base.OnClosed(e);
    }
}