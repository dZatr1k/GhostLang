using System.Windows;
using GhostLang.Application.Interfaces;
using GhostLang.WPF.ViewModels;
using GhostLang.WPF.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace GhostLang.WPF.Services;

public class OverlayWindowService(IServiceProvider serviceProvider, SettingsViewModel settings)
    : IOverlayWindowService
{
    private CaptureOverlayWindow? _overlayWindow;

    public MainWindowViewModel? ViewModelContext { get; set; }

    public void ShowSelectionDialog()
    {
        var selectionWindow = serviceProvider.GetRequiredService<SelectionWindow>();
        if (selectionWindow.ShowDialog() == true)
        {
            settings.SelectedArea = selectionWindow.SelectedArea;
            ShowOverlay();
        }
    }

    public void ShowOverlay()
    {
        _overlayWindow?.Close();
        _overlayWindow = serviceProvider.GetRequiredService<CaptureOverlayWindow>();
        
        if (ViewModelContext != null)
            _overlayWindow.DataContext = ViewModelContext; 
            
        _overlayWindow.InitializePositionAndSize(settings.SelectedArea);
        _overlayWindow.Show();
    }

    public void CloseOverlay()
    {
        _overlayWindow?.Close();
        _overlayWindow = null;
    }

    public void MoveOverlayX(double deltaX)
    {
        if (_overlayWindow == null || settings.SelectedArea.IsEmpty) return;

        var screenWidth = SystemParameters.PrimaryScreenWidth;

        var newLeft = _overlayWindow.Left + deltaX;
        newLeft = Math.Max(0, Math.Min(newLeft, screenWidth - _overlayWindow.Width));
        _overlayWindow.SmoothMove(newLeft);

        var r = settings.SelectedArea;
        settings.SelectedArea = r with { X = newLeft };
    }

    public void MoveOverlayY(double deltaY)
    {
        if (_overlayWindow == null || settings.SelectedArea.IsEmpty) return;
        
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        var newTop = _overlayWindow.Top + deltaY;

        newTop = Math.Max(0, Math.Min(newTop, screenHeight - _overlayWindow.Height));

        _overlayWindow.SmoothMove(null, newTop);

        var r = settings.SelectedArea;
        settings.SelectedArea = r with { Y = newTop };
    }

    public void ResizeOverlayWidth(double deltaW)
    {
        if (_overlayWindow == null || settings.SelectedArea.IsEmpty) return;
        
        var newWidth = _overlayWindow.Width + deltaW;
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        newWidth = Math.Max(50, Math.Min(newWidth, screenWidth - _overlayWindow.Left));
        _overlayWindow.SmoothResize(newWidth);

        var r = settings.SelectedArea;
        settings.SelectedArea = r with { Width = newWidth };
    }
    
    public void ResizeOverlayHeight(double deltaH)
    {
        if (_overlayWindow == null || settings.SelectedArea.IsEmpty) return;
        
        var newHeight = _overlayWindow.Height + deltaH;

        var screenHeight = SystemParameters.PrimaryScreenHeight;
        newHeight = Math.Max(50, Math.Min(newHeight, screenHeight - _overlayWindow.Top));
        _overlayWindow.SmoothResize(null, newHeight);

        var r = settings.SelectedArea;
        settings.SelectedArea = r with { Height = newHeight };
    }
}