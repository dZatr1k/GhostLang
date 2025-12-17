using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;
using System.Windows.Threading;
using GhostLang.WPF.ViewModels;

namespace GhostLang.WPF.Windows;

public partial class CaptureOverlayWindow
{
    private readonly SettingsViewModel _settingsViewModel;
    private const double ToolbarHeight = 30;
    
    private readonly DispatcherTimer _updateDebounceTimer;

    private HwndSource? _hwndSource;

    public CaptureOverlayWindow(SettingsViewModel settingsViewModel)
    {
        _settingsViewModel = settingsViewModel;
        InitializeComponent();
        
        LocationChanged += OnOverlayMovedOrResized;
        SizeChanged += OnOverlayMovedOrResized;
            
        _updateDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateDebounceTimer.Tick += OnDebounceTimerTick;
    }
    
    private void Toolbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
    
    private void OnDebounceTimerTick(object? sender, EventArgs e)
    {
        _updateDebounceTimer.Stop();
        UpdateSelectedArea();
    }

    private void UpdateSelectedArea()
    {
        _settingsViewModel.SelectedArea = new Rect(
            Left,
            Top + ToolbarHeight,
            Width,
            Height - ToolbarHeight
        );
    }
    
    private void OnOverlayMovedOrResized(object? sender, EventArgs e)
    {
        _updateDebounceTimer.Stop();
        _updateDebounceTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        LocationChanged -= OnOverlayMovedOrResized;
        SizeChanged -= OnOverlayMovedOrResized;

        _updateDebounceTimer.Tick -= OnDebounceTimerTick;
        _updateDebounceTimer.Stop();
            
        base.OnClosed(e);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
    }
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_SYSCOMMAND = 0x0112;

    private enum ResizeDirection
    {
        Left = 1,
        Right = 2,
        Top = 3,
        TopLeft = 4,
        TopRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        BottomRight = 8,
    }

    private void ResizeBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;

        var rect = sender as Rectangle;
        if (rect == null) return;

        switch (rect.Name)
        {
            case "ResizeTop":
                ResizeWindow(ResizeDirection.Top);
                break;
            case "ResizeBottom":
                ResizeWindow(ResizeDirection.Bottom);
                break;
            case "ResizeLeft":
                ResizeWindow(ResizeDirection.Left);
                break;
            case "ResizeRight":
                ResizeWindow(ResizeDirection.Right);
                break;
            case "ResizeTopLeft":
                ResizeWindow(ResizeDirection.TopLeft);
                break;
            case "ResizeTopRight":
                ResizeWindow(ResizeDirection.TopRight);
                break;
            case "ResizeBottomLeft":
                ResizeWindow(ResizeDirection.BottomLeft);
                break;
            case "ResizeBottomRight":
                ResizeWindow(ResizeDirection.BottomRight);
                break;
        }
    }

    private void ResizeWindow(ResizeDirection direction)
    {
        if (_hwndSource == null) return;

        SendMessage(_hwndSource.Handle, WM_SYSCOMMAND, 0xF000 + (int)direction, IntPtr.Zero);
    }

    public void InitializePositionAndSize(Rect area)
    {
        _settingsViewModel.SelectedArea = area;
        Left = _settingsViewModel.SelectedArea.X;
        Top = _settingsViewModel.SelectedArea.Y - ToolbarHeight;
        Width = _settingsViewModel.SelectedArea.Width;
        Height = _settingsViewModel.SelectedArea.Height + ToolbarHeight;
    }
}