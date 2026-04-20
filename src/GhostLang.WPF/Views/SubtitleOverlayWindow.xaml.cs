using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GhostLang.WPF.Services;

namespace GhostLang.WPF.Views;

public partial class SubtitleOverlayWindow : Window
{
    private readonly DispatcherTimer _hideTimer;
    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(150));
    private bool _showOriginal = true;
    private string _position = "Bottom";
    private int _monitorIndex = -1;
    private int _minDurationMs = 1500;
    private int _maxDurationMs = 8000;
    private int _maxCharsBeforeEarlyHide = 400;

    public SubtitleOverlayWindow()
    {
        InitializeComponent();

        _hideTimer = new DispatcherTimer();
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            FadeOutAndHide();
        };

        Loaded += (_, _) => RepositionToScreen();
        SizeChanged += (_, _) => RepositionToScreen();
    }

    public void Configure(bool showOriginal, string position, int monitorIndex,
        int minDurationMs, int maxDurationMs, int maxCharsBeforeEarlyHide)
    {
        _showOriginal = showOriginal;
        _position = string.IsNullOrWhiteSpace(position) ? "Bottom" : position;
        _monitorIndex = monitorIndex;
        _minDurationMs = Math.Max(200, minDurationMs);
        _maxDurationMs = Math.Max(_minDurationMs, maxDurationMs);
        _maxCharsBeforeEarlyHide = Math.Max(50, maxCharsBeforeEarlyHide);
    }

    public void ShowSubtitle(string original, string translated, long segmentDurationMs = 0)
    {
        OriginalText.Text = original ?? string.Empty;
        OriginalText.Visibility = _showOriginal && !string.IsNullOrWhiteSpace(original)
            ? Visibility.Visible
            : Visibility.Collapsed;

        TranslatedText.Text = translated ?? string.Empty;

        if (!IsVisible)
        {

            Opacity = 0;
            Show();
            FadeIn();
        }
        else
        {

            BeginAnimation(OpacityProperty, null);
            Opacity = 1;
        }

        RepositionToScreen();

        var totalChars = (original?.Length ?? 0) + (translated?.Length ?? 0);
        int durationMs;
        if (totalChars > _maxCharsBeforeEarlyHide)
        {
            durationMs = _minDurationMs;
        }
        else
        {
            const int readingBufferMs = 1000;
            var baseMs = segmentDurationMs > 0 ? segmentDurationMs + readingBufferMs : _minDurationMs;
            durationMs = (int)Math.Clamp(baseMs, _minDurationMs, _maxDurationMs);
        }

        _hideTimer.Stop();
        _hideTimer.Interval = TimeSpan.FromMilliseconds(durationMs);
        _hideTimer.Start();
    }

    public void HideSubtitle()
    {
        _hideTimer.Stop();
        FadeOutAndHide();
    }

    public void ToggleSubtitle()
    {
        if (IsVisible)
        {
            HideSubtitle();
        }
        else
        {
            Opacity = 0;
            Show();
            FadeIn();

            if (_hideTimer.Interval.TotalMilliseconds > 0)
            {
                _hideTimer.Stop();
                _hideTimer.Start();
            }
        }
    }

    private void FadeIn()
    {
        var anim = new DoubleAnimation(1.0, FadeDuration) { FillBehavior = FillBehavior.Stop };

        anim.Completed += (_, _) => { BeginAnimation(OpacityProperty, null); Opacity = 1.0; };
        BeginAnimation(OpacityProperty, anim);
    }

    private void FadeOutAndHide()
    {
        var anim = new DoubleAnimation(0.0, FadeDuration) { FillBehavior = FillBehavior.Stop };
        anim.Completed += (_, _) =>
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = 0;
            Hide();
        };
        BeginAnimation(OpacityProperty, anim);
    }

    private void RepositionToScreen()
    {
        if (ActualWidth <= 0) return;
        var workArea = GetTargetWorkAreaInDiu();
        const double margin = 40.0;

        Left = _position switch
        {
            "TopLeft" or "BottomLeft" => workArea.Left + margin,
            "TopRight" or "BottomRight" => workArea.Right - ActualWidth - margin,
            _ => workArea.Left + (workArea.Width - ActualWidth) / 2
        };

        Top = _position.StartsWith("Top")
            ? workArea.Top + margin
            : workArea.Bottom - ActualHeight - margin;
    }

    private Rect GetTargetWorkAreaInDiu()
    {
        var monitors = MonitorEnumeration.EnumerateMonitors();
        if (monitors.Count == 0)
            return SystemParameters.WorkArea;

        var target = _monitorIndex >= 0 && _monitorIndex < monitors.Count
            ? monitors[_monitorIndex]
            : (monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0]);

        double dpiX = 1.0, dpiY = 1.0;
        try
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            dpiX = dpi.DpiScaleX;
            dpiY = dpi.DpiScaleY;
        }
        catch { }

        var b = target.WorkArea;
        return new Rect(b.X / dpiX, b.Y / dpiY, b.Width / dpiX, b.Height / dpiY);
    }
}
