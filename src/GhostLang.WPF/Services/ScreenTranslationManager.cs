using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows.Threading;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services;
using GhostLang.Core.Settings;

namespace GhostLang.WPF.Services;

public class ScreenTranslationManager(
    IScreenCaptureService captureService,
    IPipelineBuilder pipelineBuilder,
    IConfigurationService configService) : IScreenTranslationManager
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private CaptureRegion? _region;
    private SupportedLanguage _targetLanguage;
    private List<SupportedLanguage> _sourceLanguages = [];
    private volatile bool _isProcessing;
    private int _frameCount;
    private byte[]? _lastFrameHash;
    private byte[]? _lastFrameBytes;
    private int _unchangedCount;
    private volatile bool _skipNextNewFrame;

    private const double MajorChangeThreshold = 0.30;

    public event Action<TranslationContext>? FrameProcessed;
    public event Action<string>? StatusChanged;
    public event Action? BeforeCapture;
    public event Action? AfterCapture;
    public event Action? MajorContentChanged;

    public bool RecordingMode { get; set; }

    public void Start(CaptureRegion region, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguages)
    {
        _region = region;
        _targetLanguage = targetLanguage;
        _sourceLanguages = sourceLanguages;
        _frameCount = 0;
        _lastFrameHash = null;
        _lastFrameBytes = null;
        _unchangedCount = 0;

        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();

        StatusChanged?.Invoke("Started, waiting for first frame...");
    }

    public void Stop()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _isProcessing = false;
        _lastFrameHash = null;
        _lastFrameBytes = null;
        StatusChanged?.Invoke("Stopped");
    }

    public void UpdateRegion(CaptureRegion region)
    {
        _region = region;
        _lastFrameHash = null;
        _lastFrameBytes = null;
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
                StatusChanged?.Invoke("Empty frame");
                return;
            }

            var currentHash = MD5.HashData(imageBytes);
            if (_lastFrameHash != null && currentHash.AsSpan().SequenceEqual(_lastFrameHash))
            {
                _unchangedCount++;
                StatusChanged?.Invoke($"Frame #{_frameCount}: unchanged (x{_unchangedCount})");
                return;
            }

            if (_skipNextNewFrame)
            {
                _skipNextNewFrame = false;
                _lastFrameHash = currentHash;
                _lastFrameBytes = imageBytes;
                _unchangedCount = 0;
                StatusChanged?.Invoke($"Frame #{_frameCount}: baseline (post-render)");
                return;
            }

            if (_lastFrameBytes != null)
            {
                var changeRatio = CalculateChangeRatio(_lastFrameBytes, imageBytes);

                if (changeRatio > MajorChangeThreshold)
                {
                    MajorContentChanged?.Invoke();
                    StatusChanged?.Invoke($"Major change ({changeRatio:P0}), clearing...");
                }
            }

            _lastFrameHash = currentHash;
            _lastFrameBytes = imageBytes;
            _unchangedCount = 0;

            StatusChanged?.Invoke($"Processing ({imageBytes.Length / 1024} KB)...");

            var config = configService.Load();
            var pipeline = pipelineBuilder.BuildImagePipeline(config);

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
                        StatusChanged?.Invoke($"Frame #{_frameCount}: stale ({sw.ElapsedMilliseconds}ms), skip");
                        _lastFrameHash = null;
                        return;
                    }
                }
            }

            var fragments = context.TextFragments?.Count ?? 0;
            var rendered = context.TextFragments?.Count(f => f.RenderedPatch is { Length: > 0 }) ?? 0;
            var mode = RecordingMode ? " [REC]" : "";
            StatusChanged?.Invoke($"Frame #{_frameCount}: {sw.ElapsedMilliseconds}ms, fragments: {fragments}, rendered: {rendered}{mode}");

            FrameProcessed?.Invoke(context);
            _skipNextNewFrame = true;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Error: {ex.Message}");
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