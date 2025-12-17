using System.ComponentModel;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GhostLang.WPF.Commands;
using GhostLang.WPF.DI;
using GhostLang.WPF.Helpers;
using GhostLang.WPF.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace GhostLang.WPF.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly ITranslationContextFactory _translationContextFactory;
    private CaptureOverlayWindow? _overlayWindow;
    
    private CancellationTokenSource _cancellationTokenSource;
    private readonly DispatcherTimer _timer;
    private ITranslationContext? _translationContext;
    private bool _isUpdating = false;
    
    private string _statusText = "Остановлено";
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }
    
    private string _translatedText = String.Empty;
    public string TranslatedText
    {
        get => _translatedText;
        set => SetField(ref _translatedText, value);
    }
    
    private BitmapSource? _lastCapturedImage;
    public BitmapSource? LastCapturedImage
    {
        get => _lastCapturedImage;
        set => SetField(ref _lastCapturedImage, value);
    }

    private bool _isProcessRunning = false;
    public bool IsProcessRunning
    {
        get => _isProcessRunning;
        set
        {
            if (SetField(ref _isProcessRunning, value))
            {
                (StartCommand as RelayCommand)?.CanExecute(null);
                (StopCommand as RelayCommand)?.CanExecute(null);
            }
        }
    }
    
    public ICommand SelectAreaCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    

    public MainWindowViewModel(
        IServiceProvider serviceProvider, 
        SettingsViewModel settingsViewModel, 
        ITranslationContextFactory translationContextFactory)
    {
        _serviceProvider = serviceProvider;
        _settingsViewModel = settingsViewModel;
        _translationContextFactory = translationContextFactory;
        _cancellationTokenSource = new();

        StartCommand = new RelayCommand(
            execute: _ => StartTranslation(), 
            canExecute: _ => !IsProcessRunning && !_settingsViewModel.SelectedArea.IsEmpty
        );
        
        StopCommand = new RelayCommand(
            execute: _ => StopTranslation(),
            canExecute: _ => IsProcessRunning
        );
        
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        
        SelectAreaCommand = new RelayCommand(_ => SelectArea());
        
        _timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(_settingsViewModel.TimerIntervalMilliseconds)
        };

        _timer.Tick += Timer_Tick;
        
        _settingsViewModel.PropertyChanged += OnSettingsChanged;
    }
    
    private async void Timer_Tick(object? sender, EventArgs e)
    {
        if (_isUpdating) 
            return;

        if (_translationContext is null || _settingsViewModel.SelectedArea.IsEmpty)
            return;
            
        if (_cancellationTokenSource.IsCancellationRequested) return;

        _isUpdating = true;
        try
        {
            var (image, text) = await Task.Run(() => UpdateTranslation(_cancellationTokenSource.Token));

            LastCapturedImage = image;
            TranslatedText = text;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Остановка...";
        }
        catch (Exception ex)
        {
            StopTranslation();
            StatusText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            _isUpdating = false;
        }
    }
    
    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.TimerIntervalMilliseconds))
        {
            _timer.Interval = TimeSpan.FromMilliseconds(_settingsViewModel.TimerIntervalMilliseconds);
        }
        
        if (e.PropertyName == nameof(SettingsViewModel.SelectedArea))
        {
            (StartCommand as RelayCommand)?.CanExecute(null);
        }
    }
    
    private void SelectArea()
    {
        var selectionWindow = _serviceProvider.GetRequiredService<SelectionWindow>();
        if (selectionWindow.ShowDialog() == true)
        {
            var selectedArea = selectionWindow.SelectedArea;
            _overlayWindow?.Close();
            _overlayWindow = _serviceProvider.GetRequiredService<CaptureOverlayWindow>();
            _overlayWindow.InitializePositionAndSize(selectedArea);
            _overlayWindow.Show();
        }
    }
    
    private void OpenSettings()
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.ShowDialog();
    }

    private void StartTranslation()
    {
        if (_translationContext is not null) return;
        
        _translationContext = _translationContextFactory.CreateContext();
        _cancellationTokenSource = new(); 
        
        IsProcessRunning = true; 
        StatusText = "Работает...";
        _timer.Start();
    }

    private void StopTranslation()
    {
        if(_translationContext is null) return;
        
        _timer.Stop();
        
        _translationContext.Dispose();
        _translationContext = null;
        
        IsProcessRunning = false; 
        
        _cancellationTokenSource.Cancel(); 
        _cancellationTokenSource.Dispose(); 
        
        StatusText = "Остановлено";
    }

    private (BitmapSource Image, string Text) UpdateTranslation(CancellationToken cancellationToken)
    {
        if (_translationContext is null)
            throw new InvalidOperationException("Translation scope is not initialized.");

        using Bitmap screenshot = _translationContext.ScreenCaptureService.CaptureScreenArea(RectHelper.ToDrawingRectangle(_settingsViewModel.SelectedArea));
        var wpfImage = BitmapHelper.ConvertToBitmapSource(screenshot);
         
        var translatedText = _translationContext.TranslationUseCase
            .Translate(screenshot, cancellationToken)
            .GetAwaiter()
            .GetResult();

        return (wpfImage, translatedText);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
        _translationContext?.Dispose();
        _settingsViewModel.PropertyChanged -= OnSettingsChanged;
    }
}