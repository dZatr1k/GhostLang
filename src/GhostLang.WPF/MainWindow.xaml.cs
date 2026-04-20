using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GhostLang.WPF.Services;
using GhostLang.WPF.ViewModels;
using GhostLang.WPF.Views;

namespace GhostLang.WPF;

public partial class MainWindow : Window
{
    private readonly GlobalHotKeyService _hotKeyService;
    private readonly MainViewModel _viewModel;
    private bool _sidebarExpanded;
    private bool _closeConfirmed;

    private const double CollapsedWidth = 48;
    private const double ExpandedWidth = 200;
    private static readonly Duration AnimDuration = new(TimeSpan.FromMilliseconds(150));
    private static readonly IEasingFunction Easing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

    public MainWindow(MainViewModel viewModel, GlobalHotKeyService hotKeyService, ThemeService themeService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _hotKeyService = hotKeyService;

        SourceInitialized += (_, _) => _hotKeyService.Register(this);
        Closing += OnClosing;
        Closed += (_, _) => _hotKeyService.Dispose();

        UpdateSidebarLogo(themeService);
        themeService.ThemeChanged += () => UpdateSidebarLogo(themeService);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed) return;

        if (_viewModel.IsAnyPipelineActive)
        {
            var loc = LocalizationService.Instance;
            var title = loc?["Shutdown_Title"] ?? "Confirm exit";
            var message = loc?["Shutdown_ActivePipelineMessage"]
                          ?? "Translation is active. Exit will stop it. Continue?";
            var yesText = loc?["Shutdown_ExitButton"] ?? "Exit";
            var noText = loc?["Shutdown_CancelButton"] ?? "Cancel";

            var dialog = new ConfirmationDialog(title, message, yesText, noText)
            {
                Owner = this
            };
            dialog.ShowDialog();

            if (!dialog.Confirmed)
            {
                e.Cancel = true;
                return;
            }
        }

        e.Cancel = true;
        _ = Dispatcher.BeginInvoke(async () =>
        {
            try
            {
                await _viewModel.ShutdownAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}");
            }
            _closeConfirmed = true;
            Close();
        });
    }

    private void UpdateSidebarLogo(ThemeService themeService)
    {
        LabelAppName.Source = new Uri(themeService.LogoTextPath, UriKind.Relative);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        AnimateSidebar(_sidebarExpanded);
    }

    private void AnimateSidebar(bool expand)
    {
        var targetWidth = expand ? ExpandedWidth : CollapsedWidth;
        var targetOpacity = expand ? 1.0 : 0.0;
        var targetMargin = expand ? ExpandedWidth - 42 : 6;

        var widthAnim = new DoubleAnimation(targetWidth, AnimDuration) { EasingFunction = Easing };
        SidebarBorder.BeginAnimation(WidthProperty, widthAnim);

        var opacityAnim = new DoubleAnimation(targetOpacity, AnimDuration) { EasingFunction = Easing };
        LabelHome.BeginAnimation(OpacityProperty, opacityAnim);
        LabelSettings.BeginAnimation(OpacityProperty, opacityAnim);
        LabelDebug.BeginAnimation(OpacityProperty, opacityAnim);
        LabelBenchmark.BeginAnimation(OpacityProperty, opacityAnim);
        LabelAppName.BeginAnimation(OpacityProperty, opacityAnim);

        var marginAnim = new ThicknessAnimation(
            new Thickness(targetMargin, 0, 0, 0), AnimDuration) { EasingFunction = Easing };
        ToggleSidebarButton.BeginAnimation(MarginProperty, marginAnim);

        ToggleIconPath.Data = (System.Windows.Media.Geometry)FindResource(
            expand ? "PanelOpenGeometry" : "PanelCloseGeometry");
        var loc = LocalizationService.Instance;
        ToggleSidebarButton.ToolTip = expand ? loc?["Nav_CollapsePanel"] : loc?["Nav_ExpandPanel"];
    }

    private void NavHome_Click(object sender, RoutedEventArgs e)
        => _viewModel.NavigateMainCommand.Execute(null);

    private void NavSettings_Click(object sender, RoutedEventArgs e)
        => _viewModel.NavigateSettingsCommand.Execute(null);

    private void NavDebug_Click(object sender, RoutedEventArgs e)
        => _viewModel.NavigateDebugCommand.Execute(null);

    private void NavBenchmark_Click(object sender, RoutedEventArgs e)
        => _viewModel.NavigateBenchmarkCommand.Execute(null);

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
        => Close();
}
