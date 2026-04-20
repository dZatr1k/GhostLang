using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services;
using GhostLang.WPF.Services;

namespace GhostLang.WPF.Views;

public partial class WorkWindow : Window
{
    private const int ToolbarHeight = 32;

    private readonly CaptureRegion _region;

    public event Action? StartRequested;
    public event Action? StopRequested;
    public event Action? CopyTranslatedTextRequested;
    public event Action? SaveCurrentFrameRequested;
    public event Action? ForceRefreshRequested;
    public event Action<SupportedLanguage>? SwitchTargetLanguageRequested;
    public event Action<bool>? ToggleOriginalVisibilityRequested;

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
        PopulateLanguageSubmenu();

        SourceInitialized += (_, _) => WindowCaptureExclusion.ExcludeFromCapture(this);

        OverflowMenu.Opened += (_, _) => WindowCaptureExclusion.ExcludeFromCapture(OverflowMenu);
        OverflowTooltip.Opened += (_, _) => WindowCaptureExclusion.ExcludeFromCapture(OverflowTooltip);

        Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(OverflowMenu, OnMouseDownOutsideOverflowMenu);
    }

    private bool _suppressNextOverflowClick;

    private void OnMouseDownOutsideOverflowMenu(object sender, MouseButtonEventArgs e)
    {
        var pos = Mouse.GetPosition(OverflowButton);
        var bounds = new Rect(0, 0, OverflowButton.ActualWidth, OverflowButton.ActualHeight);
        if (bounds.Contains(pos))
            _suppressNextOverflowClick = true;
    }

    private void PopulateLanguageSubmenu()
    {
        SwitchLangMenuItem.Items.Clear();
        foreach (var lang in Enum.GetValues<SupportedLanguage>()
                     .Where(l => l != SupportedLanguage.Unknown))
        {
            var item = new MenuItem
            {
                Header = lang.ToString(),
                IsCheckable = true,
                Tag = lang
            };
            item.Click += SwitchLang_Click;
            SwitchLangMenuItem.Items.Add(item);
        }
    }

    public void SetCurrentTargetLanguage(SupportedLanguage current)
    {
        foreach (var item in SwitchLangMenuItem.Items.OfType<MenuItem>())
        {
            item.IsChecked = item.Tag is SupportedLanguage l && l == current;
        }
    }

    private void OverflowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_suppressNextOverflowClick)
        {
            _suppressNextOverflowClick = false;
            return;
        }

        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
        => CopyTranslatedTextRequested?.Invoke();

    private void SaveFrame_Click(object sender, RoutedEventArgs e)
        => SaveCurrentFrameRequested?.Invoke();

    private void Refresh_Click(object sender, RoutedEventArgs e)
        => ForceRefreshRequested?.Invoke();

    private void SwitchLang_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: SupportedLanguage lang }) return;
        foreach (var item in SwitchLangMenuItem.Items.OfType<MenuItem>())
            item.IsChecked = ReferenceEquals(item, sender);
        SwitchTargetLanguageRequested?.Invoke(lang);
    }

    private void ToggleOriginal_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi)
            ToggleOriginalVisibilityRequested?.Invoke(mi.IsChecked);
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

    private void StartButton_Click(object sender, RoutedEventArgs e) => RequestStart();

    private void StopButton_Click(object sender, RoutedEventArgs e) => RequestStop();

    public void RequestStart()
    {
        if (IsRunning) return;
        SetRunningUi(true);
        StartRequested?.Invoke();
    }

    public void RequestStop()
    {
        if (!IsRunning) return;
        SetRunningUi(false);
        StopRequested?.Invoke();
    }

    private void SetRunningUi(bool running)
    {
        IsRunning = running;
        StartButton.IsEnabled = !running;
        StopButton.IsEnabled = running;
        StatusLabel.Text = running ? "Running..." : "Stopped";
        StatusLabel.Foreground = running
            ? FindResource("SuccessBrush") as System.Windows.Media.Brush
            : FindResource("SecondaryTextBrush") as System.Windows.Media.Brush;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        StopRequested?.Invoke();
        Close();
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
