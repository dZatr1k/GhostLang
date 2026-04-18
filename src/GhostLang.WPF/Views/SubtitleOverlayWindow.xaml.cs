using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using GhostLang.WPF.Services;

namespace GhostLang.WPF.Views;

public partial class SubtitleOverlayWindow : Window
{
    private readonly DispatcherTimer _hideTimer;
    private bool _showOriginal = true;
    private string _position = "Bottom";
    private int _monitorIndex = -1;

    public SubtitleOverlayWindow()
    {
        InitializeComponent();

        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            Hide();
        };

        Loaded += (_, _) => RepositionToScreen();
        SizeChanged += (_, _) => RepositionToScreen();
    }

    public void Configure(bool showOriginal, string position, int monitorIndex)
    {
        _showOriginal = showOriginal;
        _position = string.IsNullOrWhiteSpace(position) ? "Bottom" : position;
        _monitorIndex = monitorIndex;
    }

    public void ShowSubtitle(string original, string translated)
    {
        OriginalText.Text = original ?? string.Empty;
        OriginalText.Visibility = _showOriginal && !string.IsNullOrWhiteSpace(original)
            ? Visibility.Visible
            : Visibility.Collapsed;

        TranslatedText.Text = translated ?? string.Empty;

        if (!IsVisible)
            Show();

        RepositionToScreen();

        _hideTimer.Stop();
        _hideTimer.Start();
    }

    public void HideSubtitle()
    {
        _hideTimer.Stop();
        Hide();
    }

    private void RepositionToScreen()
    {
        var workArea = GetTargetWorkAreaInDiu();

        if (ActualWidth > 0)
            Left = workArea.Left + (workArea.Width - ActualWidth) / 2;

        Top = _position == "Top"
            ? workArea.Top + 40
            : workArea.Bottom - ActualHeight - 40;
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