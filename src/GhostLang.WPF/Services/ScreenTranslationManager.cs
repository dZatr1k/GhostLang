using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows.Threading;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Steps.Implementations;
using GhostLang.Core.Services;
using GhostLang.Core.Settings;

namespace GhostLang.WPF.Services;

public class ScreenTranslationManager(
    IScreenCaptureService captureService,
    IPipelineBuilder pipelineBuilder,
    IConfigurationService configService,
    MotionDetectionStep motionDetectionStep) : IScreenTranslationManager
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private IImageTranslationPipeline? _pipeline;
    private CaptureRegion? _region;
    private SupportedLanguage _targetLanguage;
    private List<SupportedLanguage> _sourceLanguages = [];
    private volatile bool _isProcessing;
    private int _frameCount;
    private byte[]? _lastFrameHash;
    private byte[]? _lastFrameBytes;
    private int _unchangedCount;
    private volatile bool _skipNextNewFrame;

    private bool _adaptiveFpsEnabled;
    private int _fastIntervalMs;
    private int _slowIntervalMs;
    private int _stableFramesToSlowDown;
    private bool _inSlowMode;

    private const int MajorChangeHysteresis = 2;

    private int _consecutiveMajorChanges;

    public event Action<TranslationContext>? FrameProcessed;
    public event Action<PipelineStatus>? StatusChanged;
    public event Action? BeforeCapture;
    public event Action? AfterCapture;
    public event Action? MajorContentChanged;

    public bool RecordingMode { get; set; }

    public bool IsActive => _timer.IsEnabled;

    public void TriggerImmediateProcess()
    {
        if (!_timer.IsEnabled) return;
        OnTick(this, EventArgs.Empty);
    }

    public void Start(CaptureRegion region, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguages)
    {
        _region = region;
        _targetLanguage = targetLanguage;
        _sourceLanguages = sourceLanguages;
        _frameCount = 0;
        _lastFrameHash = null;
        _lastFrameBytes = null;
        _unchangedCount = 0;

        _skipNextNewFrame = false;

        motionDetectionStep.Reset();

        var config = configService.Load();

        _pipeline?.Dispose();
        _pipeline = pipelineBuilder.BuildImagePipeline(config);

        _adaptiveFpsEnabled = config.AdaptiveFpsEnabled;
        _fastIntervalMs = Math.Clamp(config.ScreenFastIntervalMs, 100, 2000);
        _slowIntervalMs = Math.Clamp(config.ScreenSlowIntervalMs, 500, 5000);
        if (_slowIntervalMs < _fastIntervalMs) _slowIntervalMs = _fastIntervalMs;
        _stableFramesToSlowDown = Math.Clamp(config.ScreenStableFramesToSlowDown, 1, 20);
        _inSlowMode = false;
        _timer.Interval = TimeSpan.FromMilliseconds(_fastIntervalMs);

        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();

        _consecutiveMajorChanges = 0;
        StatusChanged?.Invoke(new PipelineStatus.Started());
    }

    public void Stop()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _isProcessing = false;
        _lastFrameHash = null;
        _lastFrameBytes = null;
        _skipNextNewFrame = false;
        _consecutiveMajorChanges = 0;

        _pipeline?.Dispose();
        _pipeline = null;
        StatusChanged?.Invoke(new PipelineStatus.Stopped());
    }

    public void UpdateRegion(CaptureRegion region)
    {
        _region = region;
        _lastFrameHash = null;
        _lastFrameBytes = null;

        if (_adaptiveFpsEnabled && _inSlowMode)
            SwitchToFastMode();
    }

    private void SwitchToSlowMode()
    {
        _inSlowMode = true;
        _timer.Interval = TimeSpan.FromMilliseconds(_slowIntervalMs);
    }

    private void SwitchToFastMode()
    {
        _inSlowMode = false;
        _timer.Interval = TimeSpan.FromMilliseconds(_fastIntervalMs);
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        if (_isProcessing || _region == null)
            return;

        _isProcessing = true;

        try
        {
            if (RecordingMode)
            {
                BeforeCapture?.Invoke();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    () => { }, DispatcherPriority.Render);
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    () => { }, DispatcherPriority.Background);
                await Task.Delay(16);
            }

            var region = _region;
            var imageBytes = await Task.Run(() =>
                captureService.CaptureRegion(region.X, region.Y, region.Width, region.Height));

            if (RecordingMode)
            {
                AfterCapture?.Invoke();
            }

            if (imageBytes.Length == 0)
            {
                StatusChanged?.Invoke(new PipelineStatus.FrameEmpty());
                return;
            }

            var currentHash = MD5.HashData(imageBytes);
            if (_lastFrameHash != null && currentHash.AsSpan().SequenceEqual(_lastFrameHash))
            {
                _unchangedCount++;

                if (_adaptiveFpsEnabled && !_inSlowMode && _unchangedCount >= _stableFramesToSlowDown)
                    SwitchToSlowMode();
                StatusChanged?.Invoke(new PipelineStatus.FrameUnchanged(_frameCount, _unchangedCount));
                return;
            }

            if (_skipNextNewFrame)
            {
                _skipNextNewFrame = false;
                _lastFrameHash = currentHash;
                _lastFrameBytes = imageBytes;
                _unchangedCount = 0;
                StatusChanged?.Invoke(new PipelineStatus.FrameBaseline(_frameCount));
                return;
            }

            if (_lastFrameBytes != null)
            {
                var changeRatio = CalculateChangeRatio(_lastFrameBytes, imageBytes);
                var threshold = Math.Clamp(configService.Load().MajorContentChangeThreshold, 0.05, 0.80);

                if (changeRatio > threshold)
                {
                    _consecutiveMajorChanges++;
                    if (_consecutiveMajorChanges >= MajorChangeHysteresis)
                    {
                        MajorContentChanged?.Invoke();
                        StatusChanged?.Invoke(new PipelineStatus.MajorContentChanged(changeRatio, _consecutiveMajorChanges));
                        _consecutiveMajorChanges = 0;
                    }
                    else
                    {
                        StatusChanged?.Invoke(new PipelineStatus.PossibleChange(changeRatio));
                    }
                }
                else
                {
                    _consecutiveMajorChanges = 0;
                }
            }

            _lastFrameHash = currentHash;
            _lastFrameBytes = imageBytes;
            _unchangedCount = 0;

            if (_adaptiveFpsEnabled && _inSlowMode)
                SwitchToFastMode();

            StatusChanged?.Invoke(new PipelineStatus.FrameProcessing(imageBytes.Length / 1024));

            var pipeline = _pipeline;
            if (pipeline is null) return;

            var sw = Stopwatch.StartNew();

            var context = await Task.Run(() =>
                pipeline.ProcessFrameAsync(imageBytes, _targetLanguage, _sourceLanguages));

            sw.Stop();
            _frameCount++;

            var verifyRegion = _region;
            if (verifyRegion != null)
            {
                var verifyBytes = await Task.Run(() =>
                    captureService.CaptureRegion(verifyRegion.X, verifyRegion.Y, verifyRegion.Width, verifyRegion.Height));

                if (verifyBytes.Length > 0)
                {
                    var verifyHash = MD5.HashData(verifyBytes);
                    if (!verifyHash.AsSpan().SequenceEqual(currentHash))
                    {
                        StatusChanged?.Invoke(new PipelineStatus.FrameStale(_frameCount, sw.ElapsedMilliseconds));
                        _lastFrameHash = null;
                        return;
                    }
                }
            }

            var fragments = context.TextFragments?.Count ?? 0;
            var rendered = context.TextFragments?.Count(f => f.RenderedPatch is { Length: > 0 }) ?? 0;
            StatusChanged?.Invoke(new PipelineStatus.FrameProcessed(_frameCount, sw.ElapsedMilliseconds, fragments, rendered, RecordingMode));

            FrameProcessed?.Invoke(context);
            _skipNextNewFrame = true;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(new PipelineStatus.Error(ex.Message, ex));
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private static double CalculateChangeRatio(byte[] previous, byte[] current)
    {
        var minLen = Math.Min(previous.Length, current.Length);
        if (minLen == 0) return 1.0;

        const int stride = 64;
        var sampledCount = 0;
        var diffCount = 0;

        for (var i = 0; i < minLen; i += stride)
        {
            sampledCount++;
            if (previous[i] != current[i])
                diffCount++;
        }

        if (Math.Abs(previous.Length - current.Length) > minLen * 0.1)
            return 1.0;

        return sampledCount > 0 ? (double)diffCount / sampledCount : 0;
    }
}
