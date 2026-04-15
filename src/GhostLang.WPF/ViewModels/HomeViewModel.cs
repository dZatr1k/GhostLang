using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services;
using GhostLang.WPF.Services;
using GhostLang.WPF.Views;

namespace GhostLang.WPF.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IScreenTranslationManager _translationManager;
    private readonly GlobalHotKeyService _hotKeyService;

    [ObservableProperty] private string _lastRegionInfo = "";
    [ObservableProperty] private SupportedLanguage _selectedTargetLanguage = SupportedLanguage.Russian;

    private WorkWindow? _workWindow;
    private TranslationOverlayWindow? _overlayWindow;
    private bool _workWindowVisible = true;

    public IEnumerable<SupportedLanguage> TargetLanguages =>
        Enum.GetValues(typeof(SupportedLanguage)).Cast<SupportedLanguage>().Where(l => l != SupportedLanguage.Unknown);

    public HomeViewModel(IScreenTranslationManager translationManager, GlobalHotKeyService hotKeyService)
    {
        _translationManager = translationManager;
        _hotKeyService = hotKeyService;

        _translationManager.FrameProcessed += OnFrameProcessed;
        _translationManager.StatusChanged += OnStatusChanged;
        _translationManager.BeforeCapture += () => _overlayWindow?.HideOverlay();
        _translationManager.AfterCapture += () => _overlayWindow?.ShowOverlay();
        _translationManager.MajorContentChanged += () =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => _overlayWindow?.ClearOverlay());

        _hotKeyService.ToggleVisibility += OnToggleVisibility;
        _hotKeyService.MoveRequested += OnMoveRequested;
        _hotKeyService.ResizeRequested += OnResizeRequested;
    }

    private void OnToggleVisibility()
    {
        if (_workWindow == null) return;

        _workWindowVisible = !_workWindowVisible;

        if (_workWindowVisible)
            _workWindow.Show();
        else
            _workWindow.Hide();
    }

    private void OnMoveRequested(int deltaX, int deltaY)
    {
        if (_workWindow == null) return;

        _workWindow.Left += deltaX;
        _workWindow.Top += deltaY;
        SyncOverlayPosition();
    }

    private void OnResizeRequested(int deltaW, int deltaH)
    {
        if (_workWindow == null) return;

        var newWidth = _workWindow.Width + deltaW;
        var newHeight = _workWindow.Height + deltaH;

        if (newWidth >= 50) _workWindow.Width = newWidth;
        if (newHeight >= 50) _workWindow.Height = newHeight;
        SyncOverlayPosition();
    }

    private void OnRecordingModeChanged(bool isRecording)
    {
        _translationManager.RecordingMode = isRecording;

        if (_overlayWindow != null)
        {
            if (isRecording)
                WindowCaptureExclusion.IncludeInCapture(_overlayWindow);
            else
                WindowCaptureExclusion.ExcludeFromCapture(_overlayWindow);
        }

        // Force re-capture on mode change
        _translationManager.UpdateRegion(_workWindow!.CaptureRegion);
    }

    private void OnStatusChanged(string status)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _workWindow?.UpdateStatus(status);
        });
    }

    [RelayCommand]
    private void SelectScreenRegion()
    {
        var selectionWindow = new RegionSelectionWindow();
        var result = selectionWindow.ShowDialog();

        if (result != true || selectionWindow.SelectedRegion is not { } region)
            return;

        CleanupWindows();

        _workWindow = new WorkWindow(region);
        _workWindowVisible = true;
        _workWindow.StartRequested += OnStartRequested;
        _workWindow.StopRequested += OnStopRequested;
        _workWindow.RecordingModeChanged += OnRecordingModeChanged;
        _workWindow.Closed += (_, _) => CleanupWindows();
        _workWindow.LocationChanged += (_, _) => SyncOverlayPosition();
        _workWindow.SizeChanged += (_, _) => SyncOverlayPosition();
        _workWindow.Show();
    }

    private void OnStartRequested()
    {
        if (_workWindow == null) return;

        _workWindow.UpdateRegionFromWindow();
        var region = _workWindow.CaptureRegion;

        _overlayWindow?.Close();
        _overlayWindow = new TranslationOverlayWindow(region);
        _overlayWindow.Show();

        _translationManager.Start(region, SelectedTargetLanguage,
            [SupportedLanguage.English, SupportedLanguage.Russian]);
    }

    private void OnStopRequested()
    {
        _translationManager.Stop();
        _overlayWindow?.ClearOverlay();
    }

    private void OnFrameProcessed(TranslationContext context)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _overlayWindow?.RenderFrame(context);
        });
    }

    private void SyncOverlayPosition()
    {
        if (_workWindow == null) return;

        _workWindow.UpdateRegionFromWindow();
        var region = _workWindow.CaptureRegion;
        _overlayWindow?.UpdateRegion(region);
        _translationManager.UpdateRegion(region);
    }

    private void CleanupWindows()
    {
        _translationManager.Stop();

        if (_workWindow != null)
        {
            _workWindow.StartRequested -= OnStartRequested;
            _workWindow.StopRequested -= OnStopRequested;
            _workWindow.Close();
            _workWindow = null;
        }

        _overlayWindow?.Close();
        _overlayWindow = null;
        _workWindowVisible = true;
    }
}