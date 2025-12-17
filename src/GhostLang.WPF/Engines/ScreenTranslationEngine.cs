using System.Drawing;
using System.Windows.Threading;
using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;
using GhostLang.WPF.DI;
using GhostLang.WPF.Helpers;
using GhostLang.WPF.ViewModels;
using OpenCvSharp.WpfExtensions;

namespace GhostLang.WPF.Engines;

public class ScreenTranslationEngine : IScreenTranslatorEngine
{
    private readonly SettingsViewModel _settings;
    private readonly ITranslationContextFactory _contextFactory;
    private DispatcherTimer _timer;
    private CancellationTokenSource? _cts;
    private ITranslationContext? _translationContext;
    private bool _isUpdating;

    public event EventHandler<TranslationResultArgs>? ResultReceived;
    public event EventHandler<string>? ErrorOccurred;
    public bool IsRunning { get; private set; }

    public ScreenTranslationEngine(SettingsViewModel settings, ITranslationContextFactory contextFactory)
    {
        _settings = settings;
        _contextFactory = contextFactory;
        
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
        
        _settings.PropertyChanged += (_, e) => {
             if (e.PropertyName == nameof(SettingsViewModel.TimerIntervalMilliseconds))
                 _timer.Interval = TimeSpan.FromMilliseconds(_settings.TimerIntervalMilliseconds);
        };
        _timer.Interval = TimeSpan.FromMilliseconds(_settings.TimerIntervalMilliseconds);
    }

    public void Start()
    {
        if (IsRunning) return;
        
        _translationContext = _contextFactory.CreateContext();
        _cts = new CancellationTokenSource();
        IsRunning = true;
        _timer.Start();
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _timer.Stop();
        _cts?.Cancel();
        _translationContext?.Dispose();
        _translationContext = null;
        IsRunning = false;
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (_isUpdating)
                return;

            if (_translationContext is null || _settings.SelectedArea.IsEmpty)
                return;

            if (_cts is null or { IsCancellationRequested: true }) return;

            _isUpdating = true;
            try
            {
                var result = await Task.Run(() => Process(_cts.Token));
                
                ResultReceived?.Invoke(this, result);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
            finally
            {
                _isUpdating = false;
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private TranslationResultArgs Process(CancellationToken token)
    {
        if (_translationContext is null) throw new InvalidOperationException("Scope null");

        using Bitmap screenshot = _translationContext.ScreenCaptureService.CaptureScreenArea(
                _settings.SelectedArea.ToDrawingRectangle());
        var wpfImage = screenshot.ToBitmapSource();

        var blocks = _translationContext.TranslationUseCase
            .Translate(screenshot, token)
            .GetAwaiter()
            .GetResult();
        
        foreach (var block in blocks)
        {
            var blurredSource = ImageEffectsHelper.CreateInpaintedBackground(screenshot, block.Bounds.ToWpfRect());
        
            blurredSource?.Freeze(); 
        
            block.BlockBackground = blurredSource;
        }
        
        return new TranslationResultArgs
        {
            Image = wpfImage,
            TranslatedText = string.Join(Environment.NewLine, blocks.Select(b => b.Text)),
            Blocks = blocks
        };
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}