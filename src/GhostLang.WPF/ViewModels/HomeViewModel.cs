using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services;
using GhostLang.Core.Services.AudioCapture;
using GhostLang.WPF.Services;
using GhostLang.WPF.ViewModels.Settings;
using GhostLang.WPF.Views;

namespace GhostLang.WPF.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IScreenTranslationManager _translationManager;
    private readonly GlobalHotKeyService _hotKeyService;
    private readonly IConfigurationService _configService;
    private readonly PipelineValidationService _validationService;
    private readonly IAudioTranslationManager _audioManager;
    private SubtitleOverlayWindow? _subtitleOverlay;

    public event Action? NavigateToSettingsRequested;

    [ObservableProperty] private string _lastRegionInfo = "";
    [ObservableProperty] private SupportedLanguage _selectedTargetLanguage = SupportedLanguage.Russian;
    [ObservableProperty] private string _selectRegionHotKeyHint = "";

    private WorkWindow? _workWindow;
    private TranslationOverlayWindow? _overlayWindow;
    private bool _workWindowVisible = true;

    public ObservableCollection<LanguageSelectionItem> SourceLanguages { get; } = new();

    public IEnumerable<SupportedLanguage> TargetLanguages =>
        Enum.GetValues(typeof(SupportedLanguage)).Cast<SupportedLanguage>().Where(l => l != SupportedLanguage.Unknown);

    public HomeViewModel(IScreenTranslationManager translationManager, GlobalHotKeyService hotKeyService,
        IConfigurationService configService, PipelineValidationService validationService,
        IAudioTranslationManager audioManager)
    {
        _translationManager = translationManager;
        _hotKeyService = hotKeyService;
        _configService = configService;
        _validationService = validationService;
        _audioManager = audioManager;

        _translationManager.FrameProcessed += OnFrameProcessed;
        _translationManager.StatusChanged += OnStatusChanged;
        _translationManager.BeforeCapture += () => _overlayWindow?.HideOverlay();
        _translationManager.AfterCapture += () => _overlayWindow?.ShowOverlay();
        _translationManager.MajorContentChanged += () =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => _overlayWindow?.ClearOverlay());

        _audioManager.FragmentsReady += OnAudioFragmentsReady;
        _audioManager.StatusChanged += OnAudioStatusChanged;
        _audioManager.LevelChanged += OnAudioLevelChanged;

        _hotKeyService.SelectRegionRequested += OnSelectRegionRequested;
        _hotKeyService.ToggleVisibility += OnToggleVisibility;
        _hotKeyService.MoveRequested += OnMoveRequested;
        _hotKeyService.ResizeRequested += OnResizeRequested;
        _hotKeyService.BindingsReloaded += UpdateSelectRegionHotKeyHint;

        if (LocalizationService.Instance != null)
            LocalizationService.Instance.PropertyChanged += (_, _) => UpdateSelectRegionHotKeyHint();

        UpdateSelectRegionHotKeyHint();
        InitializeSourceLanguages();
    }

    private void InitializeSourceLanguages()
    {
        var langs = Enum.GetValues(typeof(SupportedLanguage)).Cast<SupportedLanguage>()
            .Where(l => l != SupportedLanguage.Unknown);

        foreach (var lang in langs)
        {
            SourceLanguages.Add(new LanguageSelectionItem
            {
                Language = lang,
                DisplayName = lang.ToString(),
                IsSelected = lang is SupportedLanguage.English or SupportedLanguage.Russian
            });
        }
    }

    private List<SupportedLanguage> GetSelectedSourceLanguages() =>
        SourceLanguages.Where(x => x.IsSelected).Select(x => x.Language).ToList();

    private void UpdateSelectRegionHotKeyHint()
    {
        var config = _configService.Load();
        var binding = config.HotKeys.FirstOrDefault(h => h.ActionId == "select_region");
        var keyCombo = binding?.ToDisplayString() ?? "—";
        var template = LocalizationService.Instance?["Home_SelectRegionHint"] ?? "Горячая клавиша: {0}";
        SelectRegionHotKeyHint = string.Format(template, keyCombo);
    }

    private void OnSelectRegionRequested()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(SelectScreenRegion);
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
        var sourceLanguages = GetSelectedSourceLanguages();

        var issues = _validationService.ValidateForStart(sourceLanguages);
        if (issues.Count > 0)
        {
            var dialog = new ValidationDialog(issues)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            dialog.ShowDialog();

            if (dialog.OpenSettingsRequested)
                NavigateToSettingsRequested?.Invoke();

            return;
        }

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

        _translationManager.Start(region, SelectedTargetLanguage, GetSelectedSourceLanguages());
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

    [ObservableProperty] private bool _isAudioActive;
    [ObservableProperty] private string _audioStatusText = string.Empty;
    [ObservableProperty] private float _audioLevelDb = -100f;

    [RelayCommand]
    private async Task ToggleAudioTranslationAsync()
    {
        if (_audioManager.IsActive)
        {
            await _audioManager.StopAsync();
            IsAudioActive = false;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _subtitleOverlay?.HideSubtitle();
            });
            return;
        }

        var config = _configService.Load();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _subtitleOverlay ??= new SubtitleOverlayWindow();
            _subtitleOverlay.Configure(
                config.SubtitleOptions.ShowOriginal,
                config.SubtitleOptions.Position,
                config.SubtitleOptions.MonitorIndex);
        });

        await _audioManager.StartAsync(
            config.ActiveAudioCaptureSource,
            SelectedTargetLanguage,
            GetSelectedSourceLanguages());

        IsAudioActive = true;
    }

    private void OnAudioFragmentsReady(object? sender, AudioTranslationSessionEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_subtitleOverlay == null)
                return;

            var original = string.Join(" ", e.Fragments.Select(f => f.OriginalText));
            var translated = string.Join(" ", e.Fragments.Select(f => f.TranslatedText));
            _subtitleOverlay.ShowSubtitle(original, translated);
        });
    }

    private void OnAudioStatusChanged(object? sender, string status)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AudioStatusText = status switch
            {
                "Active" => LocalizationService.Instance?["Home_AudioStatusActive"] ?? "Active",
                "Stopped" => LocalizationService.Instance?["Home_AudioStatusStopped"] ?? "Stopped",
                _ => status
            };
        });
    }

    private void OnAudioLevelChanged(object? sender, float levelDb)
    {
        AudioLevelDb = levelDb;
    }
}