using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Services;
using GhostLang.Core.Services.Ocr;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Settings;
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
    private string _savedConfigSnapshot = string.Empty;

    private readonly Dictionary<Type, Func<IEngineSettingsViewModel>> _engineViewModelFactories;
    private readonly System.Windows.Threading.DispatcherTimer _changeTracker;
    private bool _isRefreshingLanguage;

    [ObservableProperty] private bool _hasUnsavedChanges;


    public ObservableCollection<PipelineStepViewModel> ImagePipelineSteps { get; } = [];

    [ObservableProperty] private PipelineStepViewModel? _selectedImagePipelineStep;

    public ObservableCollection<FilterViewModel> PreProcessFilters { get; } = [];

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

    [ObservableProperty] private string _selectedTheme = "Dark";

    partial void OnSelectedThemeChanged(string value)
    {
        if (_isRefreshingLanguage) return;

        _themeService?.Apply(value);

        var config = _configService.Load();
        config.Theme = value;
        _configService.Save(config);
        UpdateSavedSnapshot();
    }

    public Dictionary<string, string> AvailableThemes { get; private set; } = BuildThemes();

    private static Dictionary<string, string> BuildThemes()
    {
        var l = LocalizationService.Instance;
        return new Dictionary<string, string>
        {
            { "Dark", l?["General_ThemeDark"] ?? "Dark" },
            { "Light", l?["General_ThemeLight"] ?? "Light" }
        };
    }

    [ObservableProperty] private string _selectedLanguage = "ru";

    partial void OnSelectedLanguageChanged(string value)
    {
        if (_isRefreshingLanguage) return;

        _localizationService?.Apply(value);

        var config = BuildCurrentConfig();
        config.Language = value;
        _configService.Save(config);

        _isRefreshingLanguage = true;
        try
        {
            RefreshLocalizedContent();
        }
        finally
        {
            _isRefreshingLanguage = false;
        }

        UpdateSavedSnapshot();
    }

    private void RefreshLocalizedContent()
    {
        AvailableThemes = BuildThemes();
        OnPropertyChanged(nameof(AvailableThemes));

        AvailableTokenModes = BuildTokenModes();
        OnPropertyChanged(nameof(AvailableTokenModes));

        CreatePipelineStructure();

        LoadAndApplySettings();

        LoadGlossary();
    }

    public Dictionary<string, string> AvailableLanguages { get; } = new()
    {
        { "ru", "Русский" },
        { "en", "English" }
    };

    private readonly GlobalHotKeyService? _hotKeyService;
    private readonly ThemeService? _themeService;
    private readonly LocalizationService? _localizationService;

    public SettingsViewModel(IConfigurationService configService, IPipelineRegistry registry,
        ITesseractModelManager modelManager, GlobalHotKeyService hotKeyService, ThemeService themeService,
        LocalizationService localizationService)
    {
        _configService = configService;
        _hotKeyService = hotKeyService;
        _themeService = themeService;
        _localizationService = localizationService;
        _registry = registry;

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
            { typeof(LibreTranslateOptions), () => new LibreTranslateSettingsViewModel() }
        };

        var initialConfig = _configService.Load();
        _selectedLanguage = initialConfig.Language;
        _selectedTheme = initialConfig.Theme;

        CreatePipelineStructure();
        LoadAndApplySettings();
        LoadGlossary();
        UpdateSavedSnapshot();

        _changeTracker = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _changeTracker.Tick += (_, _) => CheckForChanges();
    }

    public void StartChangeTracking() => _changeTracker.Start();

    public void StopChangeTracking()
    {
        _changeTracker.Stop();
        CheckForChanges();
    }

    private void LoadGlossary()
    {
        var config = _configService.Load();
        GlossaryTokenMode = config.GlossaryTokenMode;
        CacheTtlMinutes = config.CacheTtlMinutes;
        CacheMaxCharacters = config.CacheMaxCharacters;

        HotKeyBindings.Clear();
        foreach (var hk in config.HotKeys)
        {
            var vm = new HotKeyBindingViewModel();
            vm.LoadFrom(hk);
            HotKeyBindings.Add(vm);
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

        SelectedTheme = config.Theme;
        SelectedLanguage = config.Language;
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
        var stepDescriptors = _registry.GetImagePipelineSteps();

        foreach (var desc in stepDescriptors.OrderBy(d => d.Order))
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
            ImagePipelineSteps.Add(stepVm);
        }

        SelectedImagePipelineStep = ImagePipelineSteps.FirstOrDefault();
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

    private static string SerializeConfig(AppConfig config)
    {
        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = false });
    }

    private void UpdateSavedSnapshot()
    {
        _savedConfigSnapshot = SerializeConfig(BuildCurrentConfig());
        HasUnsavedChanges = false;
    }

    public void CheckForChanges()
    {
        try
        {
            var currentJson = SerializeConfig(BuildCurrentConfig());
            HasUnsavedChanges = currentJson != _savedConfigSnapshot;
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var config = BuildCurrentConfig();
            _configService.Save(config);
            _hotKeyService?.ReloadBindings();
            _themeService?.Apply(config.Theme);
            UpdateSavedSnapshot();

            HandyControl.Controls.Growl.Success(new HandyControl.Data.GrowlInfo
            {
                Message = LocalizationService.Instance?["Settings_SavedSuccess"] ?? "Saved!",
                WaitTime = 3,
                StaysOpen = false,
                Token = "MainGrowl"
            });
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
}