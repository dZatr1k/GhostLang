using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines;
using GhostLang.WPF.Services;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;
using GhostLang.Core.Services;
using GhostLang.Core.Services.AudioCapture;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Ocr;
using GhostLang.WPF.ViewModels.Settings;
using GhostLang.WPF.Views;
using Microsoft.Win32;

namespace GhostLang.WPF.ViewModels;

public partial class DebugViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly IPipelineBuilder _pipelineBuilder;
    private readonly IOcrEngineFactory _ocrEngineFactory;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IAudioCaptureServiceFactory _audioCaptureFactory;
    private readonly LanguageCapabilityService _capabilityService;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ProcessImageCommand))]
    private byte[]? _currentImageBytes;

    [ObservableProperty] private BitmapFrame? _originalImageSource;
    [ObservableProperty] private BitmapFrame? _preprocessedImageSource;

    [ObservableProperty] private BitmapFrame? _resultImageSource;

    [ObservableProperty] private FlowDocument _appConfigDocument = new();

    public ObservableCollection<PipelineStepInfo> ScreenPipelineStepsInfo { get; } = new();
    public ObservableCollection<PipelineStepInfo> AudioPipelineStepsInfo { get; } = new();

    [ObservableProperty] private int _selectedDebugTabIndex;

    [ObservableProperty] private bool _isProcessing;

    [ObservableProperty] private bool _isAppConfigExpanded = true;
    [ObservableProperty] private bool _isScreenPipelineExpanded;
    [ObservableProperty] private bool _isAudioPipelineExpanded;

    private bool _isAdjustingAccordion;

    private static readonly JsonSerializerOptions _prettyJson = new() { WriteIndented = true };

    partial void OnSelectedDebugTabIndexChanged(int value)
    {
        if (value == 2) RefreshConfiguration();
    }

    partial void OnIsAppConfigExpandedChanged(bool value)
    {
        if (value) CollapseOthers(keepAppConfig: true, keepScreen: false, keepAudio: false);
    }

    partial void OnIsScreenPipelineExpandedChanged(bool value)
    {
        if (value) CollapseOthers(keepAppConfig: false, keepScreen: true, keepAudio: false);
    }

    partial void OnIsAudioPipelineExpandedChanged(bool value)
    {
        if (value) CollapseOthers(keepAppConfig: false, keepScreen: false, keepAudio: true);
    }

    private void CollapseOthers(bool keepAppConfig, bool keepScreen, bool keepAudio)
    {
        if (_isAdjustingAccordion) return;
        _isAdjustingAccordion = true;
        try
        {
            if (!keepAppConfig) IsAppConfigExpanded = false;
            if (!keepScreen) IsScreenPipelineExpanded = false;
            if (!keepAudio) IsAudioPipelineExpanded = false;
        }
        finally
        {
            _isAdjustingAccordion = false;
        }
    }

    public ObservableCollection<SupportedLanguage> AvailableLanguages { get; } = new();

    [ObservableProperty] private double _originalImageWidth;
    [ObservableProperty] private double _originalImageHeight;

    public ObservableCollection<StepMetricViewModel> ScreenMetricsView { get; } = new();
    public ObservableCollection<StepMetricViewModel> AudioMetricsView { get; } = new();

    [ObservableProperty] private long _screenPipelineTotalMs;
    [ObservableProperty] private long _audioPipelineTotalMs;

    public ObservableCollection<AudioFragment> AudioPipelineFragments { get; } = new();

    public ObservableCollection<SupportedLanguage> SourceLanguages { get; } = new();

    [ObservableProperty] private SupportedLanguage _selectedSourceLanguage = SupportedLanguage.English;
    [ObservableProperty] private SupportedLanguage _selectedTargetLanguage = SupportedLanguage.English;

    public ObservableCollection<SupportedLanguage> TargetLanguages { get; } = new();

    public ObservableCollection<RenderedFragmentViewModel> RenderedFragments { get; } = new();

    public DebugViewModel(IConfigurationService configService, IPipelineBuilder pipelineBuilder,
        IOcrEngineFactory ocrEngineFactory, IScreenCaptureService screenCaptureService,
        IAudioCaptureServiceFactory audioCaptureFactory, ThemeService themeService,
        LanguageCapabilityService capabilityService)
    {
        _configService = configService;
        _pipelineBuilder = pipelineBuilder;
        _ocrEngineFactory = ocrEngineFactory;
        _screenCaptureService = screenCaptureService;
        _audioCaptureFactory = audioCaptureFactory;
        _capabilityService = capabilityService;

        themeService.ThemeChanged += RefreshConfiguration;
        _capabilityService.Changed += () =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => RebuildLanguageLists(initial: false));

        if (LocalizationService.Instance != null)
            LocalizationService.Instance.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(SourceLanguages));
                OnPropertyChanged(nameof(TargetLanguages));
            };

        RefreshConfiguration();
        RebuildLanguageLists(initial: true);
    }

    private void RebuildLanguageLists(bool initial)
    {
        var available = new HashSet<SupportedLanguage>();
        foreach (var l in _capabilityService.GetScreenLanguages()) available.Add(l);
        foreach (var l in _capabilityService.GetAudioLanguages()) available.Add(l);
        if (available.Count == 0)
            foreach (var l in LanguageCapabilitySets.AllTwenty) available.Add(l);

        SourceLanguages.Clear();
        AvailableLanguages.Clear();
        TargetLanguages.Clear();
        foreach (var lang in Enum.GetValues<SupportedLanguage>())
        {
            if (lang == SupportedLanguage.Unknown || !available.Contains(lang)) continue;
            SourceLanguages.Add(lang);
            AvailableLanguages.Add(lang);
            TargetLanguages.Add(lang);
        }

        if (!available.Contains(SelectedSourceLanguage))
        {
            SelectedSourceLanguage = available.Contains(SupportedLanguage.English)
                ? SupportedLanguage.English
                : available.First();
        }

        if (!available.Contains(SelectedTargetLanguage))
        {
            SelectedTargetLanguage = available.Contains(SupportedLanguage.English)
                ? SupportedLanguage.English
                : available.First();
        }
    }

    private void RefreshConfiguration()
    {
        var config = _configService.Load();
        var json = JsonSerializer.Serialize(config, _prettyJson);
        AppConfigDocument = JsonSyntaxHighlighter.Build(json);

        ScreenPipelineStepsInfo.Clear();
        foreach (var info in _pipelineBuilder.DescribeImagePipeline(config))
            ScreenPipelineStepsInfo.Add(info);

        AudioPipelineStepsInfo.Clear();
        foreach (var info in _pipelineBuilder.DescribeAudioPipeline(config))
            AudioPipelineStepsInfo.Add(info);
    }

    [RelayCommand]
    private void LoadImage()
    {
        var openFileDialog = new OpenFileDialog { Filter = "Images (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg" };
        if (openFileDialog.ShowDialog() == true)
        {
            LoadImageBytes(File.ReadAllBytes(openFileDialog.FileName));
        }
    }

    [RelayCommand]
    private void CaptureRegion()
    {
        var selectionWindow = new RegionSelectionWindow();
        var result = selectionWindow.ShowDialog();

        if (result != true || selectionWindow.SelectedRegion is not { } region)
            return;

        var imageBytes = _screenCaptureService.CaptureRegion(region.X, region.Y, region.Width, region.Height);
        if (imageBytes.Length > 0)
        {
            LoadImageBytes(imageBytes);
        }
    }

    private void LoadImageBytes(byte[] imageBytes)
    {
        CurrentImageBytes = imageBytes;

        var frame = BitmapFrame.Create(new MemoryStream(imageBytes),
            BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        frame.Freeze();

        OriginalImageSource = frame;
        OriginalImageWidth = frame.PixelWidth;
        OriginalImageHeight = frame.PixelHeight;
        PreprocessedImageSource = null;
        ResultImageSource = null;
        RenderedFragments.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanProcessImage))]
    private async Task ProcessImageAsync()
    {
        if (CurrentImageBytes == null) return;

        var selectedSourceLangs = new List<SupportedLanguage> { SelectedSourceLanguage };

        if (SelectedSourceLanguage == SupportedLanguage.Unknown)
        {
            MessageBox.Show(LocalizationService.Instance?["Debug_SelectSourceLang"] ?? "Select source language",
                LocalizationService.Instance?["Debug_Warning"] ?? "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var config = _configService.Load();

        if (config.ActiveOcrEngine != null)
        {
            try
            {
                var engine = _ocrEngineFactory.Create(config.ActiveOcrEngine);

                foreach (var lang in selectedSourceLangs)
                {
                    var isSupported = await engine.IsLanguageSupportedAsync(lang);
                    if (!isSupported)
                    {
                        var msg = string.Format(LocalizationService.Instance?["Debug_OcrLangNotFound"] ?? "Language pack for '{0}' not found.", lang);
                        MessageBox.Show(msg,
                            LocalizationService.Instance?["Debug_OcrError"] ?? "OCR Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = string.Format(LocalizationService.Instance?["Debug_InitError"] ?? "OCR init error: {0}", ex.Message);
                MessageBox.Show(msg, LocalizationService.Instance?["Debug_Error"] ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        var dynamicPipeline = _pipelineBuilder.BuildImagePipeline(config);

        IsProcessing = true;
        TranslationContext context;
        try
        {
            context = await dynamicPipeline.ProcessFrameAsync(CurrentImageBytes, SelectedTargetLanguage, selectedSourceLangs);
        }
        finally
        {
            IsProcessing = false;
        }

        if (context.ProcessedImage is { Length: > 0 })
        {
            var prepFrame = BitmapFrame.Create(new MemoryStream(context.ProcessedImage),
                BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            prepFrame.Freeze();
            PreprocessedImageSource = prepFrame;
        }
        else
        {
            PreprocessedImageSource = null;
        }

        RebuildMetrics(ScreenMetricsView, context.Metrics, out var screenTotalMs);
        ScreenPipelineTotalMs = screenTotalMs;

        RenderedFragments.Clear();

        foreach (var fragment in context.TextFragments)
        {
            if (fragment.RenderedPatch is not { Length: > 0 }) continue;

            var patchBitmap = BitmapFrame.Create(new MemoryStream(fragment.RenderedPatch),
                BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            patchBitmap.Freeze();

            RenderedFragments.Add(new RenderedFragmentViewModel
            {
                Image = patchBitmap,
                X = fragment.Bounds.X,
                Y = fragment.Bounds.Y,
                Width = fragment.Bounds.Width,
                Height = fragment.Bounds.Height
            });
        }

        ResultImageSource = RenderCompositeResult();
    }

    private bool CanProcessImage() => CurrentImageBytes != null;

    public IEnumerable<AudioCaptureSource> AudioSources =>
        Enum.GetValues(typeof(AudioCaptureSource)).Cast<AudioCaptureSource>();

    [ObservableProperty] private AudioCaptureSource _selectedAudioSource = AudioCaptureSource.Microphone;

    [ObservableProperty] private string _audioTestResultText = string.Empty;
    [ObservableProperty] private bool _hasAudioTestResult;
    [ObservableProperty] private string _audioRawAsrText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecordAudioTestCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadAudioFileCommand))]
    private bool _isAudioRecording;

    [ObservableProperty] private string _audioInfoText = string.Empty;

    [RelayCommand(CanExecute = nameof(CanRecordAudioTest))]
    private async Task RecordAudioTestAsync()
    {
        IsAudioRecording = true;

        try
        {
            using var service = _audioCaptureFactory.Create(SelectedAudioSource);
            var pcm = await service.CaptureForDurationAsync(TimeSpan.FromSeconds(5));

            var path = Path.Combine(
                Path.GetTempPath(),
                $"ghostlang-audio-{SelectedAudioSource}-{DateTime.Now:yyyyMMdd-HHmmss}.wav");

            PcmWavWriter.WritePcm16Mono(path, pcm, service.SampleRate);

            await ProcessAudioPcmAsync(pcm, service.SampleRate, service.ChannelCount, path);
        }
        catch (Exception ex)
        {
            var template = LocalizationService.Instance?["Debug_AudioTestError"] ?? "Error: {0}";
            AudioTestResultText = string.Format(template, ex.Message);
            HasAudioTestResult = true;
        }
        finally
        {
            IsAudioRecording = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRecordAudioTest))]
    private async Task LoadAudioFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Audio files (*.wav;*.mp3;*.m4a;*.flac;*.ogg)|*.wav;*.mp3;*.m4a;*.flac;*.ogg|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        IsAudioRecording = true;
        try
        {
            var pcm = await Task.Run(() => AudioFileLoader.LoadAsPcm16Mono16kHz(dialog.FileName));
            await ProcessAudioPcmAsync(pcm, AudioFileLoader.TargetSampleRate, AudioFileLoader.TargetChannels, dialog.FileName);
        }
        catch (Exception ex)
        {
            var template = LocalizationService.Instance?["Debug_AudioTestError"] ?? "Error: {0}";
            AudioTestResultText = string.Format(template, ex.Message);
            HasAudioTestResult = true;
        }
        finally
        {
            IsAudioRecording = false;
        }
    }

    private async Task ProcessAudioPcmAsync(byte[] pcm, int sampleRate, int channelCount, string sourceLabel)
    {
        HasAudioTestResult = false;
        AudioTestResultText = string.Empty;
        AudioPipelineFragments.Clear();
        AudioMetricsView.Clear();
        AudioPipelineTotalMs = 0;

        var durationSeconds = pcm.Length / (double)(sampleRate * channelCount * 2);
        AudioInfoText = string.Format(
            LocalizationService.Instance?["Debug_AudioInfo"] ?? "{0:F1}s • {1} Hz • {2} ch",
            durationSeconds, sampleRate, channelCount);

        var savedTemplate = LocalizationService.Instance?["Debug_AudioTestSaved"] ?? "Saved: {0}";
        var savedLine = string.Format(savedTemplate, sourceLabel);

        var transcribingLine = LocalizationService.Instance?["Debug_AudioTranscribing"] ?? "Transcribing...";
        AudioTestResultText = savedLine + "\n\n" + transcribingLine;
        HasAudioTestResult = true;

        var selectedSourceLangs = new List<SupportedLanguage> { SelectedSourceLanguage };

        var config = _configService.Load();
        var pipeline = _pipelineBuilder.BuildAudioPipeline(config);

        var audioContext = await pipeline.ProcessAsync(
            pcm, sampleRate, channelCount,
            SelectedTargetLanguage, selectedSourceLangs);

        foreach (var fragment in audioContext.AudioFragments)
        {
            AudioPipelineFragments.Add(fragment);
        }

        RebuildMetrics(AudioMetricsView, audioContext.Metrics, out var audioTotalMs);
        AudioPipelineTotalMs = audioTotalMs;

        AudioRawAsrText = System.Text.Json.JsonSerializer.Serialize(
            audioContext.AudioFragments,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        var summary = audioContext.AudioFragments.Count == 0
            ? LocalizationService.Instance?["Debug_AudioTranscribedEmpty"] ?? "(no speech detected)"
            : string.Format(
                LocalizationService.Instance?["Debug_AudioFragmentCount"] ?? "{0} fragment(s)",
                audioContext.AudioFragments.Count);

        AudioTestResultText = savedLine + "\n\n" + summary;
    }

    private bool CanRecordAudioTest() => !IsAudioRecording;

    private void RebuildMetrics(ObservableCollection<StepMetricViewModel> target,
        IEnumerable<StepMetric> source, out long totalMs)
    {
        target.Clear();
        var list = source as IList<StepMetric> ?? source.ToList();
        totalMs = list.Sum(m => m.ElapsedMilliseconds);
        var maxMs = list.Count > 0 ? list.Max(m => m.ElapsedMilliseconds) : 0;

        for (var i = 0; i < list.Count; i++)
        {
            var m = list[i];
            target.Add(new StepMetricViewModel
            {
                Order = i + 1,
                StepName = m.StepName,
                ElapsedMilliseconds = m.ElapsedMilliseconds,
                BarWidth = maxMs > 0 ? m.ElapsedMilliseconds / (double)maxMs : 0,
                Percentage = totalMs > 0 ? m.ElapsedMilliseconds * 100.0 / totalMs : 0
            });
        }
    }

    private BitmapFrame? RenderCompositeResult()
    {
        if (OriginalImageSource == null) return null;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(OriginalImageSource,
                new Rect(0, 0, OriginalImageWidth, OriginalImageHeight));

            foreach (var fragment in RenderedFragments)
            {
                if (fragment.Image != null)
                {
                    context.DrawImage(fragment.Image,
                        new Rect(fragment.X, fragment.Y, fragment.Width, fragment.Height));
                }
            }
        }

        var rtb = new RenderTargetBitmap(
            (int)OriginalImageWidth, (int)OriginalImageHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();

        var frame = BitmapFrame.Create(rtb);
        frame.Freeze();
        return frame;
    }
}
