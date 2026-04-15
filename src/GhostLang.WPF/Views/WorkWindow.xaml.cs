using System.Windows;
using System.Windows.Input;
using GhostLang.Core.Services;
using GhostLang.WPF.Services;

namespace GhostLang.WPF.Views;

public partial class WorkWindow : Window
{
    private const int ToolbarHeight = 32;

    private readonly CaptureRegion _region;

    public event Action? StartRequested;
    public event Action? StopRequested;
    public event Action<bool>? RecordingModeChanged;

    public bool IsRunning { get; private set; }

    public CaptureRegion CaptureRegion => new()
    {
        X = _region.X,
        Y = _region.Y,
        Width = _region.Width,
        Height = _region.Height
    };

    public WorkWindow(CaptureRegion region)
    {
        InitializeComponent();
        _region = region;
        PositionFromRegion();

        SourceInitialized += (_, _) => WindowCaptureExclusion.ExcludeFromCapture(this);
    }

    private void PositionFromRegion()
    {
        var (scaleX, scaleY) = DpiHelper.GetDpiScale(this);

        Left = _region.X / scaleX;
        Top = _region.Y / scaleY - ToolbarHeight;
        Width = _region.Width / scaleX;
        Height = _region.Height / scaleY + ToolbarHeight;
    }

    public void UpdateRegionFromWindow()
    {
        var (scaleX, scaleY) = DpiHelper.GetDpiScale(this);

        _region.X = (int)(Left * scaleX);
        _region.Y = (int)((Top + ToolbarHeight) * scaleY);
        _region.Width = (int)(Width * scaleX);
        _region.Height = (int)((Height - ToolbarHeight) * scaleY);
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        IsRunning = true;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        StatusLabel.Text = "Running...";
        StatusLabel.Foreground = FindResource("SuccessBrush") as System.Windows.Media.Brush;
        StartRequested?.Invoke();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        IsRunning = false;
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        StatusLabel.Text = "Stopped";
        StatusLabel.Foreground = FindResource("SecondaryTextBrush") as System.Windows.Media.Brush;
        StopRequested?.Invoke();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        StopRequested?.Invoke();
        Close();
    }

    public bool IsRecording { get; private set; }

    private void RecButton_Click(object sender, RoutedEventArgs e)
    {
        IsRecording = !IsRecording;

        if (IsRecording)
        {
            RecButton.Background = FindResource("DangerBrush") as System.Windows.Media.Brush;
            RecButton.Foreground = System.Windows.Media.Brushes.White;
        }
        else
        {
            RecButton.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
            RecButton.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
        }

        RecordingModeChanged?.Invoke(IsRecording);
    }

    public void UpdateStatus(string status)
    {
        StatusLabel.Text = status;
    }

    private void Toolbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (IsRunning)
            UpdateRegionFromWindow();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (IsRunning)
            UpdateRegionFromWindow();
    }
}