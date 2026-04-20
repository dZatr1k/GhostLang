using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Services;
using GhostLang.Core.Services.Asr;
using GhostLang.Core.Services.Ocr;
using GhostLang.Core.Services.Vad;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Asr;
using GhostLang.Core.Settings.Audio;
using GhostLang.Core.Settings.Erasure;
using GhostLang.Core.Settings.Ocr;
using GhostLang.Core.Settings.Translation;
using GhostLang.WPF.Services;
using GhostLang.WPF.ViewModels.Settings;
using GhostLang.Core.Pipelines.Enums;
using TextRenderingMode = GhostLang.Core.Pipelines.Enums.TextRenderingMode;

namespace GhostLang.WPF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly IPipelineRegistry _registry;
    private readonly LanguageCapabilityService? _capabilityService;

    private readonly Dictionary<Type, Func<IEngineSettingsViewModel>> _engineViewModelFactories;
    private readonly System.Windows.Threading.DispatcherTimer _autosaveTimer;
    private bool _isRefreshingLanguage;
    private bool _isBulkLoading = true;

    private Type? _lastOcrOptionsType;
    private Type? _lastAsrOptionsType;
    private Type? _lastTranslationOptionsType;

    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private int _selectedPipelineSubTabIndex;

    [ObservableProperty] private bool _isShowingSavedIndicator;

    private System.Windows.Threading.DispatcherTimer? _savedIndicatorTimer;

    [RelayCommand]
    private void SelectPipelineTab(string target)
    {
        SelectedPipelineSubTabIndex = target switch
        {
            "screen" => 0,
            "audio" => 1,
            _ => SelectedPipelineSubTabIndex
        };
    }

    public void NavigateToTab(string target)
    {
        switch (target)
        {
            case "overlay":
                SelectedTabIndex = 0;
                break;
            case "pipelines":
                SelectedTabIndex = 1;
                break;
            case "screen":
                SelectedTabIndex = 1;
                SelectedPipelineSubTabIndex = 0;
                break;
            case "audio":
                SelectedTabIndex = 1;
                SelectedPipelineSubTabIndex = 1;
                break;
            case "capture":
                SelectedTabIndex = 2;
                break;
            case "hotkeys":
                SelectedTabIndex = 3;
                break;
            case "appearance":
                SelectedTabIndex = 4;
                break;
        }
    }

    public ObservableCollection<PipelineStepViewModel> ImagePipelineSteps { get; } = [];

    [ObservableProperty] private PipelineStepViewModel? _selectedImagePipelineStep;

    public ObservableCollection<PipelineStepViewModel> AudioPipelineSteps { get; } = [];

    [ObservableProperty] private PipelineStepViewModel? _selectedAudioPipelineStep;

    [ObservableProperty] private AudioCaptureSource _selectedAudioCaptureSource = AudioCaptureSource.SystemLoopback;

    public Dictionary<AudioCaptureSource, string> AvailableAudioCaptureSources { get; private set; } = BuildAudioCaptureSources();

    private static Dictionary<AudioCaptureSource, string> BuildAudioCaptureSources()
    {
        var l = LocalizationService.Instance;
        return new Dictionary<AudioCaptureSource, string>
        {
            { AudioCaptureSource.Microphone, l?["Audio_SourceMicrophone"] ?? "Microphone" },
            { AudioCaptureSource.SystemLoopback, l?["Audio_SourceLoopback"] ?? "System audio (loopback)" }
        };
    }

    [ObservableProperty] private double _silenceThresholdDb = -40.0;
    [ObservableProperty] private int _minSilenceDurationMs = 500;
    [ObservableProperty] private int _loopbackResamplerQuality = 60;
    [ObservableProperty] private Core.Settings.Audio.VadProvider _selectedVadProvider = Core.Settings.Audio.VadProvider.Rms;
    [ObservableProperty] private float _speechProbabilityThreshold = 0.5f;
    [ObservableProperty] private bool _isSileroModelDownloaded;
    [ObservableProperty] private bool _isSileroDownloading;
    [ObservableProperty] private double _sileroDownloadProgress;
    [ObservableProperty] private string _sileroDownloadStatus = string.Empty;

    public Array VadProviders => Enum.GetValues(typeof(Core.Settings.Audio.VadProvider));

    [ObservableProperty] private bool _showOriginalSubtitle = true;
    [ObservableProperty] private string _subtitlePosition = "Bottom";
    [ObservableProperty] private int _subtitleMonitorIndex = -1;
    [ObservableProperty] private int _subtitleMinDurationMs = 1500;
    [ObservableProperty] private int _subtitleMaxDurationMs = 8000;
    [ObservableProperty] private int _subtitleMaxChars = 400;
    [ObservableProperty] private bool _recordingMode;
    [ObservableProperty] private double _majorContentChangeThreshold = 0.30;
    [ObservableProperty] private bool _adaptiveFpsEnabled = true;
    [ObservableProperty] private int _screenFastIntervalMs = 200;
    [ObservableProperty] private int _screenSlowIntervalMs = 2000;

    [ObservableProperty] private VerticalAlignment _subtitlePreviewVAlign = VerticalAlignment.Bottom;
    [ObservableProperty] private HorizontalAlignment _subtitlePreviewHAlign = HorizontalAlignment.Center;

    partial void OnSubtitlePositionChanged(string value)
    {
        SubtitlePreviewVAlign = (value ?? string.Empty).StartsWith("Top")
            ? VerticalAlignment.Top
            : VerticalAlignment.Bottom;

        SubtitlePreviewHAlign = value switch
        {
            "TopLeft" or "BottomLeft" => HorizontalAlignment.Left,
            "TopRight" or "BottomRight" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center
        };
    }

    public Dictionary<string, string> AvailableSubtitlePositions { get; private set; } = BuildSubtitlePositions();

    public Dictionary<int, string> AvailableMonitors { get; private set; } = BuildMonitors();

    private static Dictionary<string, string> BuildSubtitlePositions()
    {
        var l = LocalizationService.Instance;
        return new Dictionary<string, string>
        {
            { "TopLeft", l?["Audio_SubtitlePositionTopLeft"] ?? "Top-Left" },
            { "Top", l?["Audio_SubtitlePositionTop"] ?? "Top" },
            { "TopRight", l?["Audio_SubtitlePositionTopRight"] ?? "Top-Right" },
            { "BottomLeft", l?["Audio_SubtitlePositionBottomLeft"] ?? "Bottom-Left" },
            { "Bottom", l?["Audio_SubtitlePositionBottom"] ?? "Bottom" },
            { "BottomRight", l?["Audio_SubtitlePositionBottomRight"] ?? "Bottom-Right" }
        };
    }

    private static Dictionary<int, string> BuildMonitors()
    {
        var result = new Dictionary<int, string>();
        var l = LocalizationService.Instance;
        result[-1] = l?["Audio_SubtitleMonitorPrimary"] ?? "Primary monitor (default)";

        var monitors = MonitorEnumeration.EnumerateMonitors();
        foreach (var m in monitors)
        {
            var suffix = m.IsPrimary ? " ★" : "";
            result[m.Index] = $"#{m.Index + 1}: {m.Bounds.Width}×{m.Bounds.Height}{suffix}";
        }

        return result;
    }

    public ObservableCollection<FilterViewModel> PreProcessFilters { get; } = [];

    public ObservableCollection<FilterViewModel> AudioPreProcessFilters { get; } = [];

    public ObservableCollection<GlossaryRuleViewModel> GlossaryRules { get; } = [];

    [ObservableProperty]
    private GlossaryTokenMode _glossaryTokenMode = GlossaryTokenMode.Placeholder;

    public Dictionary<GlossaryTokenMode, string> AvailableTokenModes { get; private set; } = BuildTokenModes();

    private static Dictionary<GlossaryTokenMode, string> BuildTokenModes()
    {
        var l = LocalizationService.Instance;
        return new Dictionary<GlossaryTokenMode, string>
        {
            { GlossaryTokenMode.Placeholder, l?["Glossary_ModePlaceholder"] ?? "Placeholder" },
            { GlossaryTokenMode.HtmlTag, l?["Glossary_ModeHtmlTag"] ?? "HTML Tag" }
        };
    }

    [ObservableProperty] private int _cacheTtlMinutes = 60;
    [ObservableProperty] private int _cacheMaxCharacters = 10000;

    public ObservableCollection<HotKeyBindingViewModel> HotKeyBindings { get; } = [];

    public ObservableCollection<HotKeyBindingViewModel> ImagePipelineHotKeys { get; } = [];
    public ObservableCollection<HotKeyBindingViewModel> AudioPipelineHotKeys { get; } = [];

    [ObservableProperty] private string _selectedTheme = "Dark";

    partial void OnSelectedThemeChanged(string value)
    {
        if (_isRefreshingLanguage) return;
        _themeService?.Apply(value);
    }

    public Dictionary<string, string> AvailableThemes { get; private set; } = BuildThemes();

    private static Dictionary<string, string> BuildThemes()
    {
        var l = LocalizationService.Instance;
        return new Dictionary<string, string>
        {
            { "Dark", l?["General_ThemeDark"] ?? "Dark" },
            { "Light", l?["General_ThemeLight"] ?? "Light" },
            { "System", l?["General_ThemeSystem"] ?? "Match system" }
        };
    }

    [ObservableProperty] private string _selectedLanguage = "en";

    partial void OnSelectedLanguageChanged(string value)
    {
        if (_isRefreshingLanguage) return;

        _localizationService?.Apply(value);

        _isRefreshingLanguage = true;
        try
        {
            RefreshLocalizedContent();
        }
        finally
        {
            _isRefreshingLanguage = false;
        }
    }

    private void RefreshLocalizedContent()
    {
        DetachAll();
        _isBulkLoading = true;
        try
        {
            var savedTheme = SelectedTheme;
            var savedTokenMode = GlossaryTokenMode;
            var savedCaptureSource = SelectedAudioCaptureSource;
            var savedPosition = SubtitlePosition;
            var savedMonitor = SubtitleMonitorIndex;

            AvailableThemes = BuildThemes();
            OnPropertyChanged(nameof(AvailableThemes));

            AvailableTokenModes = BuildTokenModes();
            OnPropertyChanged(nameof(AvailableTokenModes));

            AvailableAudioCaptureSources = BuildAudioCaptureSources();
            OnPropertyChanged(nameof(AvailableAudioCaptureSources));

            AvailableSubtitlePositions = BuildSubtitlePositions();
            OnPropertyChanged(nameof(AvailableSubtitlePositions));

            AvailableMonitors = BuildMonitors();
            OnPropertyChanged(nameof(AvailableMonitors));

            SelectedTheme = savedTheme;
            GlossaryTokenMode = savedTokenMode;
            SelectedAudioCaptureSource = savedCaptureSource;
            SubtitlePosition = savedPosition;
            SubtitleMonitorIndex = savedMonitor;

            CreatePipelineStructure();

            LoadAndApplySettings();

            LoadGlossary();

            OnPropertyChanged(nameof(SelectedTheme));
            OnPropertyChanged(nameof(GlossaryTokenMode));
            OnPropertyChanged(nameof(SelectedAudioCaptureSource));
            OnPropertyChanged(nameof(SubtitlePosition));
            OnPropertyChanged(nameof(SubtitleMonitorIndex));
        }
        finally
        {
            _isBulkLoading = false;
            AttachSubscriptions();
        }
    }

    public Dictionary<string, string> AvailableLanguages { get; } = new()
    {
        { "ru", "Русский" },
        { "en", "English" }
    };

    private readonly GlobalHotKeyService? _hotKeyService;
    private readonly ThemeService? _themeService;
    private readonly LocalizationService? _localizationService;
    private readonly ISileroVadModelManager? _sileroVadModelManager;

    public SettingsViewModel(IConfigurationService configService, IPipelineRegistry registry,
        ITesseractModelManager modelManager, IWhisperModelManager whisperModelManager,
        GlobalHotKeyService hotKeyService, ThemeService themeService,
        LocalizationService localizationService,
        LanguageCapabilityService capabilityService,
        ISileroVadModelManager sileroVadModelManager,
        IVoskModelManager voskModelManager)
    {
        _sileroVadModelManager = sileroVadModelManager;
        _configService = configService;
        _hotKeyService = hotKeyService;
        _themeService = themeService;
        _localizationService = localizationService;
        _registry = registry;
        _capabilityService = capabilityService;

        _engineViewModelFactories = new Dictionary<Type, Func<IEngineSettingsViewModel>>
        {
            { typeof(TesseractOcrOptions), () => new TesseractOcrViewModel(modelManager) },
            { typeof(AzureVisionOcrOptions), () => new AzureVisionOcrViewModel() },
            { typeof(OcrSpaceOptions), () => new OcrSpaceViewModel() },
            { typeof(WindowsOcrOptions), () => new WindowsOcrViewModel() },
            { typeof(TextRenderingOptions), () => new TextRenderingViewModel() },
            { typeof(SolidColorErasureOptions), () => new SolidColorErasureViewModel() },
            { typeof(OpenCvErasureOptions), () => new OpenCvErasureViewModel() },
            { typeof(GTranslateOptions), () => new GTranslateSettingsViewModel() },
            { typeof(MyMemoryOptions), () => new MyMemorySettingsViewModel() },
            { typeof(LingvaOptions), () => new LingvaSettingsViewModel() },
            { typeof(LibreTranslateOptions), () => new LibreTranslateSettingsViewModel() },
            { typeof(WhisperAsrOptions), () => new WhisperAsrSettingsViewModel(whisperModelManager) },
            { typeof(VoskAsrOptions), () => new VoskAsrSettingsViewModel(voskModelManager) },
            { typeof(AzureAsrOptions), () => new AzureAsrSettingsViewModel() }
        };

        var initialConfig = _configService.Load();
        _selectedLanguage = initialConfig.Language;
        _selectedTheme = initialConfig.Theme;

        _autosaveTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        _autosaveTimer.Tick += (_, _) =>
        {
            _autosaveTimer.Stop();
            SaveSilently();
        };

        CreatePipelineStructure();
        LoadAndApplySettings();
        LoadGlossary();

        _isBulkLoading = false;
        AttachSubscriptions();

        if (_hotKeyService != null)
            _hotKeyService.BindingsReloaded += UpdateHotKeyConflicts;

        SaveSilently(flashIndicator: false);
    }

    private void LoadGlossary()
    {
        var config = _configService.Load();
        GlossaryTokenMode = config.GlossaryTokenMode;
        CacheTtlMinutes = config.CacheTtlMinutes;
        CacheMaxCharacters = config.CacheMaxCharacters;

        HotKeyBindings.Clear();
        ImagePipelineHotKeys.Clear();
        AudioPipelineHotKeys.Clear();
        foreach (var hk in config.HotKeys)
        {
            var vm = new HotKeyBindingViewModel();
            vm.LoadFrom(hk);
            HotKeyBindings.Add(vm);
            if (hk.GroupKey == "HotKeyGroup_Audio")
                AudioPipelineHotKeys.Add(vm);
            else
                ImagePipelineHotKeys.Add(vm);
        }

        GlossaryRules.Clear();
        foreach (var rule in config.GlossaryRules)
        {
            GlossaryRules.Add(new GlossaryRuleViewModel
            {
                SourceTerm = rule.SourceTerm,
                TargetTerm = rule.TargetTerm,
                SourceVariants = string.Join(", ", rule.SourceVariants)
            });
        }
    }

    [RelayCommand]
    private void AddGlossaryRule()
    {
        GlossaryRules.Add(new GlossaryRuleViewModel());
    }

    [RelayCommand]
    private void RemoveGlossaryRule(GlossaryRuleViewModel? rule)
    {
        if (rule != null)
            GlossaryRules.Remove(rule);
    }

    private void CreatePipelineStructure()
    {
        ImagePipelineSteps.Clear();
        AudioPipelineSteps.Clear();

        BuildStepsInto(ImagePipelineSteps, _registry.GetImagePipelineSteps());
        BuildStepsInto(AudioPipelineSteps, _registry.GetAudioPipelineSteps());

        SelectedImagePipelineStep = ImagePipelineSteps.FirstOrDefault();
        SelectedAudioPipelineStep = AudioPipelineSteps.FirstOrDefault();
    }

    private void BuildStepsInto(ObservableCollection<PipelineStepViewModel> target,
        IReadOnlyList<Core.Pipelines.Descriptors.PipelineStepDescriptor> descriptors)
    {
        foreach (var desc in descriptors.OrderBy(d => d.Order))
        {
            var loc = LocalizationService.Instance;
            var stepVm = new PipelineStepViewModel
            {
                StepId = string.IsNullOrEmpty(desc.StepId) ? "unknown_step" : desc.StepId,
                StepNumber = desc.Order,
                StepName = !string.IsNullOrEmpty(desc.NameKey) && loc != null ? loc[desc.NameKey] : desc.Name,
                Description = !string.IsNullOrEmpty(desc.DescriptionKey) && loc != null ? loc[desc.DescriptionKey] : desc.Description,
                IsOptional = desc.IsOptional,
                IsEnabled = !desc.IsOptional
            };

            foreach (var engineDesc in desc.AvailableEngines)
            {
                if (_engineViewModelFactories.TryGetValue(engineDesc.OptionsType, out var factory))
                {
                    stepVm.AvailableEngines.Add(factory());
                }
            }

            if (stepVm.AvailableEngines.Any())
                stepVm.SelectedEngineViewModel = stepVm.AvailableEngines.First();

            stepVm.PropertyChanged += OnPipelineStepPropertyChanged;
            target.Add(stepVm);
        }
    }

    private bool _isSyncingGlossarySteps;

    private void OnPipelineStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PipelineStepViewModel.IsEnabled)) return;
        if (_isSyncingGlossarySteps) return;
        if (sender is not PipelineStepViewModel changed) return;

        if (changed.StepId is not ("step.image.glossary" or "step.image.glossary_restore")) return;

        var pairedId = changed.StepId == "step.image.glossary"
            ? "step.image.glossary_restore"
            : "step.image.glossary";

        var paired = ImagePipelineSteps.FirstOrDefault(s => s.StepId == pairedId);
        if (paired == null) return;

        _isSyncingGlossarySteps = true;
        paired.IsEnabled = changed.IsEnabled;
        _isSyncingGlossarySteps = false;
    }

    private void LoadAndApplySettings()
    {
        var config = _configService.Load();

        PreProcessFilters.Clear();

        var l = LocalizationService.Instance!;
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Upscale"], Description = l["Filter_UpscaleDesc"], Option = config.PreProcessOptions.Upscale, HasParameter = true });
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Blur"], Description = l["Filter_BlurDesc"], Option = config.PreProcessOptions.GaussianBlur, HasParameter = true });
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Grayscale"], Description = l["Filter_GrayscaleDesc"], Option = config.PreProcessOptions.Grayscale, HasParameter = false });
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Contrast"], Description = l["Filter_ContrastDesc"], Option = config.PreProcessOptions.Contrast, HasParameter = true });
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Binarize"], Description = l["Filter_BinarizeDesc"], Option = config.PreProcessOptions.Binarize, HasParameter = true });
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_AutoLevel"], Description = l["Filter_AutoLevelDesc"], Option = config.PreProcessOptions.AutoLevel, HasParameter = false });
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Brightness"], Description = l["Filter_BrightnessDesc"], Option = config.PreProcessOptions.Brightness, HasParameter = true });
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Sharpen"], Description = l["Filter_SharpenDesc"], Option = config.PreProcessOptions.Sharpen, HasParameter = true });
        PreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Invert"], Description = l["Filter_InvertDesc"], Option = config.PreProcessOptions.Invert, HasParameter = false });

        AudioPreProcessFilters.Clear();
        AudioPreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Audio_Normalize"], Description = l["Filter_Audio_NormalizeDesc"], Option = config.AudioPreProcessOptions.NormalizeLoudness, HasParameter = false });
        AudioPreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Audio_HighPass"], Description = l["Filter_Audio_HighPassDesc"], Option = config.AudioPreProcessOptions.HighPassFilter, HasParameter = true });
        AudioPreProcessFilters.Add(new FilterViewModel
            { DisplayName = l["Filter_Audio_NoiseSuppress"], Description = l["Filter_Audio_NoiseSuppressDesc"], Option = config.AudioPreProcessOptions.NoiseSuppression, HasParameter = false });

        foreach (var step in ImagePipelineSteps)
        {
            if (step.IsOptional && config.OptionalStepStates != null &&
                config.OptionalStepStates.TryGetValue(step.StepId, out var state))
                step.IsEnabled = state;

            if (step.StepId == "step.image.ocr" && config.ActiveOcrEngine != null)
            {
                var activeOptionsType = config.ActiveOcrEngine.GetType();
                var targetEngineVm = step.AvailableEngines.FirstOrDefault(e => e.OptionsType == activeOptionsType);

                if (targetEngineVm != null)
                {
                    targetEngineVm.ApplyOptions(config.ActiveOcrEngine);
                    step.SelectedEngineViewModel = targetEngineVm;
                }
            }

            if (step.StepId == "step.image.text_rendering" && config.TextRendering != null)
            {
                var targetEngineVm =
                    step.AvailableEngines.FirstOrDefault(e => e.OptionsType == typeof(TextRenderingOptions));

                if (targetEngineVm != null)
                {
                    targetEngineVm.ApplyOptions(config.TextRendering);
                    step.SelectedEngineViewModel = targetEngineVm;
                }
            }

            if (step.StepId == "step.image.text_erasure" && config.ActiveErasureEngine != null)
            {
                var activeOptionsType = config.ActiveErasureEngine.GetType();
                var targetEngineVm = step.AvailableEngines.FirstOrDefault(e => e.OptionsType == activeOptionsType);

                if (targetEngineVm != null)
                {
                    targetEngineVm.ApplyOptions(config.ActiveErasureEngine);
                    step.SelectedEngineViewModel = targetEngineVm;
                }
            }

            if (step.StepId == "step.image.translation" && config.ActiveTranslationEngine != null)
            {
                var activeOptionsType = config.ActiveTranslationEngine.GetType();
                var targetEngineVm = step.AvailableEngines.FirstOrDefault(e => e.OptionsType == activeOptionsType);
                if (targetEngineVm != null)
                {
                    targetEngineVm.ApplyOptions(config.ActiveTranslationEngine);
                    step.SelectedEngineViewModel = targetEngineVm;
                }
            }
        }

        foreach (var step in AudioPipelineSteps)
        {
            if (step.IsOptional && config.OptionalStepStates != null &&
                config.OptionalStepStates.TryGetValue(step.StepId, out var state))
                step.IsEnabled = state;

            if (step.StepId == "step.audio.asr" && config.ActiveAsrEngine != null)
            {
                var activeOptionsType = config.ActiveAsrEngine.GetType();
                var targetEngineVm = step.AvailableEngines.FirstOrDefault(e => e.OptionsType == activeOptionsType);
                if (targetEngineVm != null)
                {
                    targetEngineVm.ApplyOptions(config.ActiveAsrEngine);
                    step.SelectedEngineViewModel = targetEngineVm;
                }
            }
        }

        SelectedAudioCaptureSource = config.ActiveAudioCaptureSource;
        SilenceThresholdDb = config.VadOptions.SilenceThresholdDb;
        MinSilenceDurationMs = config.VadOptions.MinSilenceDurationMs;
        SelectedVadProvider = config.VadOptions.Provider;
        SpeechProbabilityThreshold = config.VadOptions.SpeechProbabilityThreshold;
        IsSileroModelDownloaded = _sileroVadModelManager?.IsModelDownloaded ?? false;
        LoopbackResamplerQuality = config.LoopbackResamplerQuality;
        ShowOriginalSubtitle = config.SubtitleOptions.ShowOriginal;
        SubtitlePosition = string.IsNullOrWhiteSpace(config.SubtitleOptions.Position) ? "Bottom" : config.SubtitleOptions.Position;
        SubtitleMonitorIndex = config.SubtitleOptions.MonitorIndex;
        SubtitleMinDurationMs = config.SubtitleOptions.MinDurationMs;
        SubtitleMaxDurationMs = config.SubtitleOptions.MaxDurationMs;
        SubtitleMaxChars = config.SubtitleOptions.MaxCharsBeforeEarlyHide;
        RecordingMode = config.RecordingMode;
        MajorContentChangeThreshold = config.MajorContentChangeThreshold;
        AdaptiveFpsEnabled = config.AdaptiveFpsEnabled;
        ScreenFastIntervalMs = config.ScreenFastIntervalMs;
        ScreenSlowIntervalMs = config.ScreenSlowIntervalMs;
    }

    private AppConfig BuildCurrentConfig()
    {
        var config = new AppConfig();

        foreach (var step in ImagePipelineSteps)
        {
            var safeId = string.IsNullOrEmpty(step.StepId) ? step.StepName : step.StepId;

            if (step.IsOptional)
            {
                config.OptionalStepStates[safeId] = step.IsEnabled;
            }

            if (step is { StepId: "step.image.ocr", SelectedEngineViewModel: not null })
                config.ActiveOcrEngine = (OcrEngineOptions)step.SelectedEngineViewModel.GetOptions();

            if (step is { StepId: "step.image.text_rendering", SelectedEngineViewModel: not null })
                config.TextRendering = (TextRenderingOptions)step.SelectedEngineViewModel.GetOptions();

            if (step is { StepId: "step.image.text_erasure", SelectedEngineViewModel: not null })
                config.ActiveErasureEngine = (ErasureEngineOptions)step.SelectedEngineViewModel.GetOptions();

            if (step is { StepId: "step.image.translation", SelectedEngineViewModel: not null })
                config.ActiveTranslationEngine = (TranslationEngineOptions)step.SelectedEngineViewModel.GetOptions();
        }

        if (PreProcessFilters.Count >= 9)
        {
            config.PreProcessOptions.Upscale = PreProcessFilters[0].Option;
            config.PreProcessOptions.GaussianBlur = PreProcessFilters[1].Option;
            config.PreProcessOptions.Grayscale = PreProcessFilters[2].Option;
            config.PreProcessOptions.Contrast = PreProcessFilters[3].Option;
            config.PreProcessOptions.Binarize = PreProcessFilters[4].Option;
            config.PreProcessOptions.AutoLevel = PreProcessFilters[5].Option;
            config.PreProcessOptions.Brightness = PreProcessFilters[6].Option;
            config.PreProcessOptions.Sharpen = PreProcessFilters[7].Option;
            config.PreProcessOptions.Invert = PreProcessFilters[8].Option;
        }

        if (AudioPreProcessFilters.Count >= 3)
        {
            config.AudioPreProcessOptions.NormalizeLoudness = AudioPreProcessFilters[0].Option;
            config.AudioPreProcessOptions.HighPassFilter = AudioPreProcessFilters[1].Option;
            config.AudioPreProcessOptions.NoiseSuppression = AudioPreProcessFilters[2].Option;
        }

        foreach (var step in AudioPipelineSteps)
        {
            var safeId = string.IsNullOrEmpty(step.StepId) ? step.StepName : step.StepId;

            if (step.IsOptional)
            {
                config.OptionalStepStates[safeId] = step.IsEnabled;
            }

            if (step is { StepId: "step.audio.asr", SelectedEngineViewModel: not null })
                config.ActiveAsrEngine = (AsrEngineOptions)step.SelectedEngineViewModel.GetOptions();
        }

        config.ActiveAudioCaptureSource = SelectedAudioCaptureSource;
        config.VadOptions = new VadOptions
        {
            SilenceThresholdDb = SilenceThresholdDb,
            MinSilenceDurationMs = MinSilenceDurationMs,
            Provider = SelectedVadProvider,
            SpeechProbabilityThreshold = SpeechProbabilityThreshold
        };
        config.LoopbackResamplerQuality = LoopbackResamplerQuality;
        config.SubtitleOptions = new SubtitleOptions
        {
            ShowOriginal = ShowOriginalSubtitle,
            Position = SubtitlePosition,
            MonitorIndex = SubtitleMonitorIndex,
            MinDurationMs = SubtitleMinDurationMs,
            MaxDurationMs = SubtitleMaxDurationMs,
            MaxCharsBeforeEarlyHide = SubtitleMaxChars
        };
        config.RecordingMode = RecordingMode;
        config.MajorContentChangeThreshold = MajorContentChangeThreshold;
        config.AdaptiveFpsEnabled = AdaptiveFpsEnabled;
        config.ScreenFastIntervalMs = ScreenFastIntervalMs;
        config.ScreenSlowIntervalMs = ScreenSlowIntervalMs;

        config.GlossaryTokenMode = GlossaryTokenMode;
        config.Theme = SelectedTheme;
        config.Language = SelectedLanguage;
        config.CacheTtlMinutes = CacheTtlMinutes;
        config.CacheMaxCharacters = CacheMaxCharacters;
        config.Theme = SelectedTheme;
        config.HotKeys = HotKeyBindings.Select(hk => hk.ToBinding()).ToList();
        config.GlossaryRules = GlossaryRules
            .Where(r => !string.IsNullOrWhiteSpace(r.SourceTerm) && !string.IsNullOrWhiteSpace(r.TargetTerm))
            .Select(r => new GlossaryRule
            {
                SourceTerm = r.SourceTerm.Trim(),
                TargetTerm = r.TargetTerm.Trim(),
                SourceVariants = r.SourceVariants
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList()
            })
            .ToList();

        return config;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _autosaveTimer.Stop();
        try
        {
            var config = BuildCurrentConfig();
            _configService.Save(config);
            var reload = _hotKeyService?.ReloadBindings();
            _themeService?.Apply(config.Theme);

            if (reload is { HasConflicts: true })
            {
                ShowHotKeyConflictGrowl(reload);
            }
            else
            {
                HandyControl.Controls.Growl.Success(new HandyControl.Data.GrowlInfo
                {
                    Message = LocalizationService.Instance?["Settings_SavedSuccess"] ?? "Saved!",
                    WaitTime = 3,
                    StaysOpen = false,
                    Token = "MainGrowl"
                });
            }
        }
        catch (Exception ex)
        {
            HandyControl.Controls.Growl.Error(new HandyControl.Data.GrowlInfo
            {
                Message = $"{LocalizationService.Instance?["Settings_SaveError"]} {ex.Message}",
                WaitTime = 5,
                StaysOpen = false,
                Token = "MainGrowl"
            });
        }
    }

    private void SaveSilently(bool flashIndicator = true)
    {
        if (_isBulkLoading) return;
        try
        {
            var config = BuildCurrentConfig();
            _configService.Save(config);
            NotifyCapabilitiesIfEngineTypeChanged(config);
            var reload = _hotKeyService?.ReloadBindings();
            if (!_isShuttingDown && reload is { HasConflicts: true })
                ShowHotKeyConflictGrowl(reload);
            if (!_isShuttingDown && flashIndicator)
                FlashSavedIndicator();
        }
        catch (Exception ex)
        {
            if (_isShuttingDown) return;
            HandyControl.Controls.Growl.Error(new HandyControl.Data.GrowlInfo
            {
                Message = $"{LocalizationService.Instance?["Settings_SaveError"]} {ex.Message}",
                WaitTime = 5,
                StaysOpen = false,
                Token = "MainGrowl"
            });
        }
    }

    private void NotifyCapabilitiesIfEngineTypeChanged(AppConfig config)
    {
        if (_capabilityService is null) return;

        var ocrType = config.ActiveOcrEngine?.GetType();
        var asrType = config.ActiveAsrEngine?.GetType();
        var trType = config.ActiveTranslationEngine?.GetType();

        var changed = ocrType != _lastOcrOptionsType
                   || asrType != _lastAsrOptionsType
                   || trType != _lastTranslationOptionsType;

        _lastOcrOptionsType = ocrType;
        _lastAsrOptionsType = asrType;
        _lastTranslationOptionsType = trType;

        if (changed)
            _capabilityService.NotifyChanged();
    }

    [RelayCommand]
    private async Task DownloadSileroModelAsync()
    {
        if (_sileroVadModelManager is null || IsSileroDownloading) return;

        IsSileroDownloading = true;
        SileroDownloadProgress = 0;
        SileroDownloadStatus = LocalizationService.Instance?["Engine_Downloading"] ?? "Downloading...";

        try
        {
            var progress = new Progress<double>(p => SileroDownloadProgress = p);
            await _sileroVadModelManager.DownloadAsync(progress);
            IsSileroModelDownloaded = _sileroVadModelManager.IsModelDownloaded;
            SileroDownloadStatus = LocalizationService.Instance?["Engine_Downloaded"] ?? "Downloaded";
        }
        catch (Exception ex)
        {
            SileroDownloadStatus = $"{LocalizationService.Instance?["Misc_Error"]} {ex.Message}";
        }
        finally
        {
            IsSileroDownloading = false;
        }
    }

    [RelayCommand]
    private void DeleteSileroModel()
    {
        if (_sileroVadModelManager is null) return;
        _sileroVadModelManager.Delete();
        IsSileroModelDownloaded = false;
        SileroDownloadStatus = LocalizationService.Instance?["Engine_NotDownloaded"] ?? "Not downloaded";
    }

    private void FlashSavedIndicator()
    {
        if (_savedIndicatorTimer == null)
        {
            _savedIndicatorTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _savedIndicatorTimer.Tick += (_, _) =>
            {
                _savedIndicatorTimer!.Stop();
                IsShowingSavedIndicator = false;
            };
        }

        _savedIndicatorTimer.Stop();
        IsShowingSavedIndicator = true;
        _savedIndicatorTimer.Start();
    }

    private void UpdateHotKeyConflicts(HotKeyReloadResult result)
    {
        var wasBulk = _isBulkLoading;
        _isBulkLoading = true;
        try
        {
            foreach (var vm in HotKeyBindings)
            {
                var conflict = result.Conflicts.FirstOrDefault(c => c.ActionId == vm.ActionId);
                vm.HasConflict = conflict != null;
            }
        }
        finally
        {
            _isBulkLoading = wasBulk;
        }
    }

    public void SuspendHotKeys() => _hotKeyService?.SuspendBindings();

    public void ResumeHotKeys() => _hotKeyService?.ResumeBindings();

    public void FlushPendingAutosave()
    {
        if (!_autosaveTimer.IsEnabled) return;
        _autosaveTimer.Stop();
        _isShuttingDown = true;
        try { SaveSilently(); }
        finally { _isShuttingDown = false; }
    }

    private bool _isShuttingDown;

    private static void ShowHotKeyConflictGrowl(HotKeyReloadResult result)
    {
        var loc = LocalizationService.Instance;
        var combos = string.Join(", ", result.Conflicts.Select(c => c.Combination).Distinct());
        var template = loc?["Settings_HotKeyConflict"] ?? "Hotkey conflict: {0}";
        HandyControl.Controls.Growl.Warning(new HandyControl.Data.GrowlInfo
        {
            Message = string.Format(template, combos),
            WaitTime = 5,
            StaysOpen = false,
            Token = "MainGrowl"
        });
    }

    private void TriggerAutosave()
    {
        if (_isBulkLoading) return;
        _autosaveTimer.Stop();
        _autosaveTimer.Start();
    }

    private static readonly HashSet<string> NonPersistedProperties = new()
    {
        nameof(SelectedTabIndex),
        nameof(SelectedPipelineSubTabIndex),
        nameof(IsShowingSavedIndicator),
        nameof(SelectedImagePipelineStep),
        nameof(SelectedAudioPipelineStep),
        nameof(SubtitlePreviewVAlign),
        nameof(SubtitlePreviewHAlign),
        nameof(HotKeyBindingViewModel.IsRecording),
        nameof(HotKeyBindingViewModel.HasConflict)
    };

    private void OnDeepChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null && NonPersistedProperties.Contains(e.PropertyName)) return;
        TriggerAutosave();
    }

    private void OnDeepCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (var item in e.OldItems) DetachItem(item);
        if (e.NewItems != null)
            foreach (var item in e.NewItems) AttachItem(item);
        TriggerAutosave();
    }

    private void AttachSubscriptions()
    {
        PropertyChanged += OnDeepChanged;

        ImagePipelineSteps.CollectionChanged += OnDeepCollectionChanged;
        foreach (var step in ImagePipelineSteps) AttachStep(step);

        AudioPipelineSteps.CollectionChanged += OnDeepCollectionChanged;
        foreach (var step in AudioPipelineSteps) AttachStep(step);

        PreProcessFilters.CollectionChanged += OnDeepCollectionChanged;
        foreach (var f in PreProcessFilters) f.PropertyChanged += OnDeepChanged;

        AudioPreProcessFilters.CollectionChanged += OnDeepCollectionChanged;
        foreach (var f in AudioPreProcessFilters) f.PropertyChanged += OnDeepChanged;

        GlossaryRules.CollectionChanged += OnDeepCollectionChanged;
        foreach (var g in GlossaryRules) g.PropertyChanged += OnDeepChanged;

        HotKeyBindings.CollectionChanged += OnDeepCollectionChanged;
        foreach (var h in HotKeyBindings) h.PropertyChanged += OnDeepChanged;
    }

    private void DetachAll()
    {
        PropertyChanged -= OnDeepChanged;

        ImagePipelineSteps.CollectionChanged -= OnDeepCollectionChanged;
        foreach (var step in ImagePipelineSteps) DetachStep(step);

        AudioPipelineSteps.CollectionChanged -= OnDeepCollectionChanged;
        foreach (var step in AudioPipelineSteps) DetachStep(step);

        PreProcessFilters.CollectionChanged -= OnDeepCollectionChanged;
        foreach (var f in PreProcessFilters) f.PropertyChanged -= OnDeepChanged;

        AudioPreProcessFilters.CollectionChanged -= OnDeepCollectionChanged;
        foreach (var f in AudioPreProcessFilters) f.PropertyChanged -= OnDeepChanged;

        GlossaryRules.CollectionChanged -= OnDeepCollectionChanged;
        foreach (var g in GlossaryRules) g.PropertyChanged -= OnDeepChanged;

        HotKeyBindings.CollectionChanged -= OnDeepCollectionChanged;
        foreach (var h in HotKeyBindings) h.PropertyChanged -= OnDeepChanged;
    }

    private void AttachItem(object? obj)
    {
        if (obj is PipelineStepViewModel step) AttachStep(step);
        else if (obj is INotifyPropertyChanged inpc) inpc.PropertyChanged += OnDeepChanged;
    }

    private void DetachItem(object? obj)
    {
        if (obj is PipelineStepViewModel step) DetachStep(step);
        else if (obj is INotifyPropertyChanged inpc) inpc.PropertyChanged -= OnDeepChanged;
    }

    private void AttachStep(PipelineStepViewModel step)
    {
        step.PropertyChanged += OnDeepChanged;
        foreach (var engine in step.AvailableEngines)
            if (engine is INotifyPropertyChanged inpc) inpc.PropertyChanged += OnDeepChanged;
    }

    private void DetachStep(PipelineStepViewModel step)
    {
        step.PropertyChanged -= OnDeepChanged;
        foreach (var engine in step.AvailableEngines)
            if (engine is INotifyPropertyChanged inpc) inpc.PropertyChanged -= OnDeepChanged;
    }
}
