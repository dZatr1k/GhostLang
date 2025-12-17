using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    
    private double _targetLeft;
    private double _targetTop;
    private double _targetWidth;
    private double _targetHeight;
    
    private bool _isAnimating;

    private const double LerpSpeed = 0.2;
    private const double SnapThreshold = 0.5;

    public CaptureOverlayWindow(SettingsViewModel settingsViewModel)
    {
        _settingsViewModel = settingsViewModel;
        InitializeComponent();
        
        _targetLeft = Left;
        _targetTop = Top;
        _targetWidth = Width;
        _targetHeight = Height;

        LocationChanged += OnOverlayMovedOrResized;
        SizeChanged += OnOverlayMovedOrResized;
            
        _updateDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateDebounceTimer.Tick += OnDebounceTimerTick;

        CompositionTarget.Rendering += OnRendering;
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
    
    private void OnRendering(object? sender, EventArgs e)
    {
        var needsMove = Math.Abs(_targetLeft - Left) > SnapThreshold || 
                        Math.Abs(_targetTop - Top) > SnapThreshold;
                         
        var needsResize = Math.Abs(_targetWidth - Width) > SnapThreshold || 
                          Math.Abs(_targetHeight - Height) > SnapThreshold;

        if (!needsMove && !needsResize) return;

        _isAnimating = true; 

        if (needsMove)
        {
            var newLeft = Left + (_targetLeft - Left) * LerpSpeed;
            var newTop = Top + (_targetTop - Top) * LerpSpeed;

            if (Math.Abs(_targetLeft - newLeft) < SnapThreshold) newLeft = _targetLeft;
            if (Math.Abs(_targetTop - newTop) < SnapThreshold) newTop = _targetTop;

            Left = newLeft;
            Top = newTop;
        }

        if (needsResize)
        {
            var newWidth = Width + (_targetWidth - Width) * LerpSpeed;
            var newHeight = Height + (_targetHeight - Height) * LerpSpeed;

            if (Math.Abs(_targetWidth - newWidth) < SnapThreshold) newWidth = _targetWidth;
            if (Math.Abs(_targetHeight - newHeight) < SnapThreshold) newHeight = _targetHeight;

            Width = newWidth;
            Height = newHeight;
        }

        _isAnimating = false;
    }
    
    private void OnOverlayMovedOrResized(object? sender, EventArgs e)
    {
        if (!_isAnimating)
        {
            _targetLeft = Left;
            _targetTop = Top;
            _targetWidth = Width;
            _targetHeight = Height;
        }

        _updateDebounceTimer.Stop();
        _updateDebounceTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        LocationChanged -= OnOverlayMovedOrResized;
        SizeChanged -= OnOverlayMovedOrResized;

        _updateDebounceTimer.Tick -= OnDebounceTimerTick;
        _updateDebounceTimer.Stop();
        
        CompositionTarget.Rendering -= OnRendering;
            
        base.OnClosed(e);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        
        if (_hwndSource != null)
        {
            SetWindowDisplayAffinity(_hwndSource.Handle, WDA_EXCLUDEFROMCAPTURE);
        }
    }
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    private const uint WM_SYSCOMMAND = 0x0112;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

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

        if (Enum.TryParse(rect.Name.Replace("Resize", ""), out ResizeDirection direction))
        {
            ResizeWindow(direction);
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
        
        _targetLeft = Left;
        _targetTop = Top;
        _targetWidth = Width;
        _targetHeight = Height;
    }
    
    public void SmoothMove(double? targetLeft = null, double? targetTop = null)
    {
        if (targetLeft.HasValue) _targetLeft = targetLeft.Value;
        if (targetTop.HasValue) _targetTop = targetTop.Value;
    }

    public void SmoothResize(double? targetWidth = null, double? targetHeight = null)
    {
        if (targetWidth.HasValue) _targetWidth = targetWidth.Value;
        if (targetHeight.HasValue) _targetHeight = targetHeight.Value;
    }
}