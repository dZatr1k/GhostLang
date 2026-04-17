using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Pipelines;
using GhostLang.WPF.Services;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
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

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ProcessImageCommand))]
    private byte[]? _currentImageBytes;

    [ObservableProperty] private BitmapFrame? _originalImageSource;
    [ObservableProperty] private BitmapFrame? _preprocessedImageSource;

    [ObservableProperty] private BitmapFrame? _resultImageSource;
    [ObservableProperty] private bool _isImageExpanded;
    [ObservableProperty] private bool _isMetricsExpanded;
    [ObservableProperty] private bool _isConfigExpanded;

    [ObservableProperty] private string _pipelineInfo = "";
    [ObservableProperty] private bool _isProcessing;

    public IEnumerable<SupportedLanguage> AvailableLanguages =>
        Enum.GetValues(typeof(SupportedLanguage)).Cast<SupportedLanguage>();

    [ObservableProperty] private double _originalImageWidth;
    [ObservableProperty] private double _originalImageHeight;
    [ObservableProperty] private ObservableCollection<StepMetric> _pipelineMetrics = new();

    public ObservableCollection<LanguageSelectionItem> SourceLanguages { get; } = new();

    [ObservableProperty] private SupportedLanguage _selectedTargetLanguage = SupportedLanguage.Unknown;

    public IEnumerable<SupportedLanguage> TargetLanguages =>
        Enum.GetValues(typeof(SupportedLanguage)).Cast<SupportedLanguage>().Where(l => l != SupportedLanguage.Unknown);

    public ObservableCollection<RenderedFragmentViewModel> RenderedFragments { get; } = new();

    public DebugViewModel(IConfigurationService configService, IPipelineBuilder pipelineBuilder,
        IOcrEngineFactory ocrEngineFactory, IScreenCaptureService screenCaptureService,
        IAudioCaptureServiceFactory audioCaptureFactory)
    {
        _configService = configService;
        _pipelineBuilder = pipelineBuilder;
        _ocrEngineFactory = ocrEngineFactory;
        _screenCaptureService = screenCaptureService;
        _audioCaptureFactory = audioCaptureFactory;

        LoadCurrentConfiguration();
        InitializeSourceLanguages();
    }

    private void InitializeSourceLanguages()
    {
        var langs = Enum.GetValues(typeof(SupportedLanguage)).Cast<SupportedLanguage>().Where(l => l != SupportedLanguage.Unknown);
        foreach (var lang in langs)
        {
            SourceLanguages.Add(new LanguageSelectionItem
            {
                Language = lang,
                DisplayName = lang.ToString(),
                IsSelected = lang == SupportedLanguage.English
            });
        }
    }

    [RelayCommand]
    private void LoadCurrentConfiguration()
    {
        var config = _configService.Load();
        var pipelineSequence = _pipelineBuilder.GetPipelineDescription(config);
        var jsonConfig = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        PipelineInfo = $"PIPELINE:\n{pipelineSequence}\n\nCONFIG:\n{jsonConfig}";
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

        var selectedSourceLangs = SourceLanguages
            .Where(x => x.IsSelected)
            .Select(x => x.Language)
            .ToList();

        if (!selectedSourceLangs.Any())
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

        PipelineMetrics.Clear();
        foreach (var metric in context.Metrics)
        {
            PipelineMetrics.Add(metric);
        }

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

        var doneMsg = string.Format(LocalizationService.Instance?["Debug_PipelineDone"] ?? "DONE (Fragments: {0}, Aborted: {1})",
            context.TextFragments?.Count ?? 0, context.IsAborted);
        PipelineInfo += $"\n\n--- {doneMsg} ---";
    }

    private bool CanProcessImage() => CurrentImageBytes != null;

    public IEnumerable<AudioCaptureSource> AudioSources =>
        Enum.GetValues(typeof(AudioCaptureSource)).Cast<AudioCaptureSource>();

    [ObservableProperty] private AudioCaptureSource _selectedAudioSource = AudioCaptureSource.Microphone;
    [ObservableProperty] private string _audioTestResultText = string.Empty;
    [ObservableProperty] private bool _hasAudioTestResult;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecordAudioTestCommand))]
    private bool _isAudioRecording;

    [RelayCommand(CanExecute = nameof(CanRecordAudioTest))]
    private async Task RecordAudioTestAsync()
    {
        IsAudioRecording = true;
        HasAudioTestResult = false;
        AudioTestResultText = string.Empty;

        try
        {
            using var service = _audioCaptureFactory.Create(SelectedAudioSource);
            var pcm = await service.CaptureForDurationAsync(TimeSpan.FromSeconds(5));

            var path = Path.Combine(
                Path.GetTempPath(),
                $"ghostlang-audio-{SelectedAudioSource}-{DateTime.Now:yyyyMMdd-HHmmss}.wav");

            PcmWavWriter.WritePcm16Mono(path, pcm, service.SampleRate);

            var template = LocalizationService.Instance?["Debug_AudioTestSaved"] ?? "Saved: {0}";
            AudioTestResultText = string.Format(template, path);
            HasAudioTestResult = true;
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

    private bool CanRecordAudioTest() => !IsAudioRecording;

    [RelayCommand]
    private void ToggleImageExpanded() => IsImageExpanded = !IsImageExpanded;

    [RelayCommand]
    private void ToggleMetricsExpanded() => IsMetricsExpanded = !IsMetricsExpanded;

    [RelayCommand]
    private void ToggleConfigExpanded() => IsConfigExpanded = !IsConfigExpanded;

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
