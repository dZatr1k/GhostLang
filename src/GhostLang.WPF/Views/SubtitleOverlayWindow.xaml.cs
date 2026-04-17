using System.Windows;
using System.Windows.Threading;

namespace GhostLang.WPF.Views;

public partial class SubtitleOverlayWindow : Window
{
    private readonly DispatcherTimer _hideTimer;
    private bool _showOriginal = true;
    private string _position = "Bottom";

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

    public void Configure(bool showOriginal, string position)
    {
        _showOriginal = showOriginal;
        _position = string.IsNullOrWhiteSpace(position) ? "Bottom" : position;
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
        var workArea = SystemParameters.WorkArea;

        if (ActualWidth > 0)
            Left = workArea.Left + (workArea.Width - ActualWidth) / 2;

        Top = _position == "Top"
            ? workArea.Top + 40
            : workArea.Bottom - ActualHeight - 40;
    }
}