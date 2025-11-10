using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;
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
    
    public ObservableCollection<BitmapSource> CapturedImages { get; } = new();

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
        _timer.Tick += async (_, _) => await UpdateTranslation();
        _settingsViewModel.PropertyChanged += OnSettingsChanged;
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
            _settingsViewModel.SelectedArea = selectionWindow.SelectedArea;
            
            _overlayWindow?.Close();

            _overlayWindow = _serviceProvider.GetRequiredService<CaptureOverlayWindow>();
            _overlayWindow.Left = _settingsViewModel.SelectedArea.Left;
            _overlayWindow.Top = _settingsViewModel.SelectedArea.Top;
            _overlayWindow.Width = _settingsViewModel.SelectedArea.Width;
            _overlayWindow.Height = _settingsViewModel.SelectedArea.Height;
            _overlayWindow.Show();
        }
    }
    
    private void OpenSettings()
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.ShowDialog();
    }
    
    public void StartTranslation()
    {
        if (_translationContext is not null) return;
        
        _translationContext = _translationContextFactory.CreateContext();
        _cancellationTokenSource = new(); 
        
        IsProcessRunning = true; 
        StatusText = "Работает...";
        _timer.Start();
    }

    public void StopTranslation()
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

    private async Task UpdateTranslation()
    {

        try
        {
            if (_translationContext is null || RectHelper.IsEmpty(_settingsViewModel.SelectedArea))
                throw new Exception("Неверная инициализация контекста перевода или область захвата не выбрана.");
            
            if (_cancellationTokenSource.IsCancellationRequested) return;
            using Bitmap screenshot = _translationContext.ScreenCaptureService.CaptureScreenArea(RectHelper.ToDrawingRectangle(_settingsViewModel.SelectedArea));

            var wpfImage = BitmapHelper.ConvertToBitmapSource(screenshot);
            CapturedImages.Add(wpfImage);
                
            if (CapturedImages.Count > 10)
            {
                CapturedImages.RemoveAt(0);
            }
            
            TranslatedText = await _translationContext.TranslationUseCase.Translate(screenshot, _cancellationTokenSource.Token);
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
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
        _translationContext?.Dispose();
        _settingsViewModel.PropertyChanged -= OnSettingsChanged;
    }
}