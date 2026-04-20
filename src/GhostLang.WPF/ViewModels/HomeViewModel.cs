using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Utilities;
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
    private readonly LanguageCapabilityService _capabilityService;
    private readonly IAudioTranslationManager _audioManager;
    private SubtitleOverlayWindow? _subtitleOverlay;

    public event Action<string?>? NavigateToSettingsRequested;

    [ObservableProperty] private string _lastRegionInfo = "";
    [ObservableProperty] private SupportedLanguage _selectedTargetLanguage = SupportedLanguage.Russian;
    [ObservableProperty] private SupportedLanguage _selectedSourceLanguage = SupportedLanguage.English;
    [ObservableProperty] private string _selectRegionHotKeyHint = "";
    [ObservableProperty] private string _audioToggleHotKeyHint = "";
    [ObservableProperty] private string _screenStatusText = "";

    private WorkWindow? _workWindow;
    private TranslationOverlayWindow? _overlayWindow;
    private bool _workWindowVisible = true;
    private bool _isCleaningUp;

    public ObservableCollection<SupportedLanguage> SourceLanguages { get; } = new();

    public ObservableCollection<string> ScreenValidationIssues { get; } = new();

    public ObservableCollection<string> AudioValidationIssues { get; } = new();

    public ObservableCollection<SupportedLanguage> TargetLanguages { get; } = new();

    public HomeViewModel(IScreenTranslationManager translationManager, GlobalHotKeyService hotKeyService,
        IConfigurationService configService, PipelineValidationService validationService,
        LanguageCapabilityService capabilityService,
        IAudioTranslationManager audioManager)
    {
        _translationManager = translationManager;
        _hotKeyService = hotKeyService;
        _configService = configService;
        _validationService = validationService;
        _capabilityService = capabilityService;
        _audioManager = audioManager;

        _translationManager.FrameProcessed += OnFrameProcessed;
        _translationManager.StatusChanged += OnStatusChanged;
        _translationManager.BeforeCapture += () => _overlayWindow?.HideOverlay();
        _translationManager.AfterCapture += () => _overlayWindow?.ShowOverlay();
        _translationManager.MajorContentChanged += () =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => _overlayWindow?.ClearOverlay());

        _audioManager.FragmentsReady += OnAudioFragmentsReady;
        _audioManager.StatusChanged += OnAudioStatusChanged;
        _audioManager.DriftChanged += OnAudioDriftChanged;

        _hotKeyService.SelectRegionRequested += OnSelectRegionRequested;
        _hotKeyService.ToggleVisibility += OnToggleVisibility;
        _hotKeyService.MoveRequested += OnMoveRequested;
        _hotKeyService.ResizeRequested += OnResizeRequested;
        _hotKeyService.BindingsReloaded += _ => UpdateHotKeyHints();
        _hotKeyService.StartStopAudioRequested += OnStartStopAudioRequested;
        _hotKeyService.ToggleSubtitleVisibilityRequested += OnToggleSubtitleVisibilityRequested;
        _hotKeyService.ScreenStartRequested += OnScreenStartRequested;
        _hotKeyService.ScreenStopRequested += OnScreenStopRequested;

        if (LocalizationService.Instance != null)
            LocalizationService.Instance.PropertyChanged += (_, _) =>
            {
                UpdateHotKeyHints();
                RefreshLanguageDisplayNames();
            };

        _capabilityService.Changed += OnCapabilitiesChanged;

        UpdateHotKeyHints();
        RebuildLanguageLists(initial: true);
    }

    private IReadOnlySet<SupportedLanguage> BuildAvailableSet()
    {
        var set = new HashSet<SupportedLanguage>();
        foreach (var l in _capabilityService.GetScreenLanguages()) set.Add(l);
        foreach (var l in _capabilityService.GetAudioLanguages()) set.Add(l);
        return set;
    }

    private void OnCapabilitiesChanged()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => RebuildLanguageLists(initial: false));
    }

    private void RebuildLanguageLists(bool initial)
    {
        var available = BuildAvailableSet();
        if (available.Count == 0)
            available = new HashSet<SupportedLanguage>(LanguageCapabilitySets.AllTwenty);

        SourceLanguages.Clear();
        TargetLanguages.Clear();
        foreach (var lang in OrderedLanguages(available))
        {
            SourceLanguages.Add(lang);
            TargetLanguages.Add(lang);
        }

        if (!available.Contains(SelectedSourceLanguage) || SelectedSourceLanguage == SupportedLanguage.Unknown)
        {
            SelectedSourceLanguage = available.Contains(SupportedLanguage.English)
                ? SupportedLanguage.English
                : available.First();
        }

        if (!available.Contains(SelectedTargetLanguage) || SelectedTargetLanguage == SupportedLanguage.Unknown)
        {
            var fallback = available.Contains(SupportedLanguage.English)
                ? SupportedLanguage.English
                : available.First();

            if (!initial)
            {
                var loc = LocalizationService.Instance;
                var msg = string.Format(
                    loc?["Home_TargetUnsupportedGrowl"] ?? "Target language {0} is not supported by current engines — switched to {1}.",
                    SelectedTargetLanguage.ToDisplayName(),
                    fallback.ToDisplayName());
                HandyControl.Controls.Growl.Warning(new HandyControl.Data.GrowlInfo
                {
                    Message = msg,
                    WaitTime = 4,
                    StaysOpen = false,
                    Token = "MainGrowl"
                });
            }

            SelectedTargetLanguage = fallback;
        }
    }

    private static IEnumerable<SupportedLanguage> OrderedLanguages(IReadOnlySet<SupportedLanguage> set) =>
        Enum.GetValues<SupportedLanguage>()
            .Where(l => l != SupportedLanguage.Unknown && set.Contains(l));

    private void RefreshLanguageDisplayNames()
    {
        OnPropertyChanged(nameof(SourceLanguages));
        OnPropertyChanged(nameof(TargetLanguages));
    }

    private List<SupportedLanguage> GetSelectedSourceLanguages() => new() { SelectedSourceLanguage };

    private void UpdateHotKeyHints()
    {
        var config = _configService.Load();
        var template = LocalizationService.Instance?["Home_HotKeyHint"] ?? "Hotkey: {0}";

        SelectRegionHotKeyHint = FormatHotKeyHint(config, "select_region", template);
        AudioToggleHotKeyHint = FormatHotKeyHint(config, "start_stop_audio", template);
    }

    private static string FormatHotKeyHint(GhostLang.Core.Settings.AppConfig config, string actionId, string template)
    {
        var binding = config.HotKeys.FirstOrDefault(h => h.ActionId == actionId);
        if (binding is null || binding.IsEmpty) return string.Empty;
        return string.Format(template, binding.ToDisplayString());
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

    private void ApplyRecordingMode()
    {
        var recording = _configService.Load().RecordingMode;
        _translationManager.RecordingMode = recording;

        if (_overlayWindow != null)
        {
            if (recording)
                WindowCaptureExclusion.IncludeInCapture(_overlayWindow);
            else
                WindowCaptureExclusion.ExcludeFromCapture(_overlayWindow);
        }
    }

    private void OnStatusChanged(PipelineStatus status)
    {
        var text = FormatScreenStatus(status);
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _workWindow?.UpdateStatus(text);
            ScreenStatusText = text;
        });
    }

    private static string FormatScreenStatus(PipelineStatus status) => status switch
    {
        PipelineStatus.Started => "Started, waiting for first frame...",
        PipelineStatus.Stopped => "Stopped",
        PipelineStatus.FrameEmpty => "Empty frame",
        PipelineStatus.FrameUnchanged u => $"Frame #{u.FrameNumber}: unchanged (x{u.StreakCount})",
        PipelineStatus.FrameBaseline b => $"Frame #{b.FrameNumber}: baseline (post-render)",
        PipelineStatus.MajorContentChanged m => $"Major change ({m.ChangeRatio:P0}, streak {m.Streak}), clearing...",
        PipelineStatus.PossibleChange p => $"Possible change ({p.ChangeRatio:P0}), waiting for confirmation...",
        PipelineStatus.FrameProcessing pr => $"Processing ({pr.SizeKb} KB)...",
        PipelineStatus.FrameStale s => $"Frame #{s.FrameNumber}: stale ({s.ElapsedMs}ms), skip",
        PipelineStatus.FrameProcessed f =>
            $"Frame #{f.FrameNumber}: {f.ElapsedMs}ms, fragments: {f.Fragments}, rendered: {f.Rendered}{(f.RecordingMode ? " [REC]" : "")}",
        PipelineStatus.Error e => $"Error: {e.Message}",
        _ => status.GetType().Name
    };

    [RelayCommand]
    private void SelectScreenRegion()
    {
        var sourceLanguages = GetSelectedSourceLanguages();

        var issues = _validationService.ValidateForStart(sourceLanguages);
        ScreenValidationIssues.Clear();
        if (issues.Count > 0)
        {
            foreach (var issue in issues) ScreenValidationIssues.Add(issue);
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
        _workWindow.CopyTranslatedTextRequested += OnCopyTranslatedTextRequested;
        _workWindow.SaveCurrentFrameRequested += OnSaveCurrentFrameRequested;
        _workWindow.ForceRefreshRequested += OnForceRefreshRequested;
        _workWindow.SwitchTargetLanguageRequested += OnSwitchTargetLanguageRequested;
        _workWindow.ToggleOriginalVisibilityRequested += OnToggleOriginalVisibilityRequested;
        _workWindow.Closed += (_, _) => CleanupWindows();
        _workWindow.LocationChanged += (_, _) => SyncOverlayPosition();
        _workWindow.SizeChanged += (_, _) => SyncOverlayPosition();
        _workWindow.SetCurrentTargetLanguage(SelectedTargetLanguage);
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

        ApplyRecordingMode();

        _translationManager.Start(region, SelectedTargetLanguage, GetSelectedSourceLanguages());
    }

    private void OnStopRequested()
    {
        _translationManager.Stop();
        _overlayWindow?.ClearOverlay();
    }

    private TranslationContext? _lastContext;

    private void OnFrameProcessed(TranslationContext context)
    {
        _lastContext = context;
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _overlayWindow?.RenderFrame(context);
        });
    }

    private void OnCopyTranslatedTextRequested()
    {
        var fragments = _lastContext?.TextFragments;
        if (fragments == null || fragments.Count == 0) return;
        var text = string.Join("\n", fragments
            .Select(f => f.TranslatedText)
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(text))
            System.Windows.Clipboard.SetText(text);
    }

    private void OnSaveCurrentFrameRequested()
    {
        var image = _lastContext?.OriginalImage;
        if (image == null || image.Length == 0) return;
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png",
            FileName = $"frame-{DateTime.Now:yyyyMMdd-HHmmss}.png"
        };
        if (dlg.ShowDialog() == true)
            System.IO.File.WriteAllBytes(dlg.FileName, image);
    }

    private void OnForceRefreshRequested()
    {
        _translationManager.TriggerImmediateProcess();
    }

    private void OnSwitchTargetLanguageRequested(SupportedLanguage lang)
    {
        SelectedTargetLanguage = lang;
        if (_translationManager.IsActive && _workWindow != null)
        {
            _translationManager.Stop();
            _translationManager.Start(_workWindow.CaptureRegion, lang, GetSelectedSourceLanguages());
        }
    }

    private void OnToggleOriginalVisibilityRequested(bool showOriginal)
    {
        _overlayWindow?.ToggleUserVisibility();
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
        if (_isCleaningUp) return;
        _isCleaningUp = true;
        try
        {
            _translationManager.Stop();

            if (_workWindow != null)
            {
                _workWindow.StartRequested -= OnStartRequested;
                _workWindow.StopRequested -= OnStopRequested;
                var w = _workWindow;
                _workWindow = null;
                w.Close();
            }

            if (_overlayWindow != null)
            {
                var o = _overlayWindow;
                _overlayWindow = null;
                o.Close();
            }

            _workWindowVisible = true;
            ScreenStatusText = string.Empty;
        }
        finally
        {
            _isCleaningUp = false;
        }
    }

    [ObservableProperty] private bool _isAudioActive;
    [ObservableProperty] private string _audioStatusText = string.Empty;

    private string _audioBaseStatus = string.Empty;
    private long _audioDriftMs;
    private bool _audioDriftCriticalNotified;

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

        var issues = _validationService.ValidateAudioForStart();
        AudioValidationIssues.Clear();
        if (issues.Count > 0)
        {
            foreach (var issue in issues) AudioValidationIssues.Add(issue);
            return;
        }

        var config = _configService.Load();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _subtitleOverlay ??= new SubtitleOverlayWindow();
            _subtitleOverlay.Configure(
                config.SubtitleOptions.ShowOriginal,
                config.SubtitleOptions.Position,
                config.SubtitleOptions.MonitorIndex,
                config.SubtitleOptions.MinDurationMs,
                config.SubtitleOptions.MaxDurationMs,
                config.SubtitleOptions.MaxCharsBeforeEarlyHide);
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

            long segmentDurationMs = 0;
            if (e.Fragments.Count > 0)
            {
                var minStart = e.Fragments.Min(f => f.StartMs);
                var maxEnd = e.Fragments.Max(f => f.EndMs);
                segmentDurationMs = Math.Max(0, maxEnd - minStart);
            }

            _subtitleOverlay.ShowSubtitle(original, translated, segmentDurationMs);
        });
    }

    private void OnAudioStatusChanged(object? sender, PipelineStatus status)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var loc = LocalizationService.Instance;
            _audioBaseStatus = status switch
            {
                PipelineStatus.Active => loc?["Home_AudioStatusActive"] ?? "Active",
                PipelineStatus.Stopped => loc?["Home_AudioStatusStopped"] ?? "Stopped",
                PipelineStatus.Error e => $"Error: {e.Message}",
                PipelineStatus.CaptureOverflow o => $"Capture overflow: dropped {o.TotalDroppedMs} ms total",
                _ => status.GetType().Name
            };

            if (status is PipelineStatus.Stopped)
            {
                _audioDriftMs = 0;
                _audioDriftCriticalNotified = false;
            }

            UpdateAudioStatusDisplay();
        });
    }

    private void OnAudioDriftChanged(object? sender, long driftMs)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _audioDriftMs = driftMs;
            UpdateAudioStatusDisplay();

            if (driftMs >= 10000 && !_audioDriftCriticalNotified)
            {
                _audioDriftCriticalNotified = true;
                var loc = LocalizationService.Instance;
                var msg = string.Format(
                    loc?["Home_AudioDriftCriticalGrowl"] ?? "Translation lagging >10s, oldest audio dropped",
                    driftMs / 1000.0);
                HandyControl.Controls.Growl.Warning(new HandyControl.Data.GrowlInfo
                {
                    Message = msg,
                    WaitTime = 4,
                    StaysOpen = false,
                    Token = "MainGrowl"
                });
            }
            else if (driftMs < 5000)
            {
                _audioDriftCriticalNotified = false;
            }
        });
    }

    private void UpdateAudioStatusDisplay()
    {
        var seconds = _audioDriftMs / 1000.0;
        var suffixTemplate = _audioDriftMs switch
        {
            >= 10000 => LocalizationService.Instance?["Home_AudioDriftCritical"] ?? " · 🔴 Lag {0:F0}s",
            >= 2000 => LocalizationService.Instance?["Home_AudioDriftWarning"] ?? " · ⚠ Lag {0:F0}s",
            _ => null
        };

        AudioStatusText = suffixTemplate is null
            ? _audioBaseStatus
            : _audioBaseStatus + string.Format(suffixTemplate, seconds);
    }

    private void OnStartStopAudioRequested()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (ToggleAudioTranslationCommand.CanExecute(null))
                ToggleAudioTranslationCommand.Execute(null);
        });
    }

    [RelayCommand]
    private void OpenSettingsFromValidation(string? target)
    {
        ScreenValidationIssues.Clear();
        AudioValidationIssues.Clear();
        NavigateToSettingsRequested?.Invoke(target);
    }

    private void OnToggleSubtitleVisibilityRequested()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _subtitleOverlay?.ToggleSubtitle();
        });
    }

    private void OnScreenStartRequested()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _workWindow?.RequestStart();
        });
    }

    private void OnScreenStopRequested()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _workWindow?.RequestStop();
        });
    }

    public bool IsAnyPipelineActive => _audioManager.IsActive || _translationManager.IsActive;

    public async Task StopAllPipelinesAsync(TimeSpan timeout)
    {
        if (_audioManager.IsActive)
        {
            var stopTask = _audioManager.StopAsync();
            var completed = await Task.WhenAny(stopTask, Task.Delay(timeout));
            if (completed != stopTask)
                System.Diagnostics.Debug.WriteLine("AudioTranslationManager.StopAsync timed out on shutdown");
        }

        _subtitleOverlay?.Close();
        _subtitleOverlay = null;
        CleanupWindows();

        IsAudioActive = false;
    }
}
