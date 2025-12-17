using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;
using GhostLang.WPF.Commands;
using GhostLang.WPF.DI;
using GhostLang.WPF.Helpers;
using GhostLang.WPF.Models;
using GhostLang.WPF.Services;
using GhostLang.WPF.Windows;
using Microsoft.Extensions.DependencyInjection;
using static System.String;

namespace GhostLang.WPF.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IScreenTranslatorEngine _screenTranslatorEngine;
    private readonly IOverlayWindowService _overlayService;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly GlobalHotkeyService _hotkeyService;

    private string _statusText = "Остановлено";

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public SettingsViewModel SettingsViewModel => _settingsViewModel;

    private string _translatedText = Empty;

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

    private bool _isProcessRunning;

    public bool IsProcessRunning
    {
        get => _isProcessRunning;
        set
        {
            if (SetField(ref _isProcessRunning, value))
            {
                (StartCommand as RelayCommand)?.CanExecute();
                (StopCommand as RelayCommand)?.CanExecute();
            }
        }
    }

    private bool _isOverlayVisible = true;

    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set => SetField(ref _isOverlayVisible, value);
    }

    public ICommand SelectAreaCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand ToggleTranslationCommand { get; }
    public ICommand CloseOverlayCommand { get; }
    public ICommand CopyTextCommand { get; }
    public ICommand ToggleOverlayVisibilityCommand { get; }


    public ObservableCollection<NavigationItem> MenuItems { get; set; } = new();

    private ViewModelBase _currentView;

    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => SetField(ref _currentView, value);
    }

    public ObservableCollection<OcrBlock> DetectedOcrBlocks { get; } = new();


    public MainWindowViewModel(
        IScreenTranslatorEngine engine,
        IOverlayWindowService overlayService,
        SettingsViewModel settingsViewModel,
        GlobalHotkeyService hotkeyService,
        IServiceProvider serviceProvider)
    {
        _screenTranslatorEngine = engine;
        _overlayService = overlayService;
        _settingsViewModel = settingsViewModel;
        _hotkeyService = hotkeyService;
        _serviceProvider = serviceProvider;

        if (_overlayService is OverlayWindowService service) service.ViewModelContext = this;

        _screenTranslatorEngine.ResultReceived += OnResultReceived;
        _screenTranslatorEngine.ErrorOccurred += (_, msg) =>
        {
            StatusText = $"Ошибка: {msg}";
            IsProcessRunning = false;
        };

        StartCommand = new RelayCommand(
            execute: _ => Start(),
            canExecute: _ => !IsProcessRunning && !_settingsViewModel.SelectedArea.IsEmpty
        );

        StopCommand = new RelayCommand(
            execute: _ => Stop(),
            canExecute: _ => IsProcessRunning
        );

        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());

        SelectAreaCommand = new RelayCommand(_ => _overlayService.ShowSelectionDialog());

        ToggleTranslationCommand = new RelayCommand(_ =>
            {
                if (IsProcessRunning)
                    Stop();
                else
                    Start();
            },
            _ => IsProcessRunning || !_settingsViewModel.SelectedArea.IsEmpty);

        CloseOverlayCommand = new RelayCommand(_ =>
        {
            Stop();
            _overlayService.CloseOverlay();
        });

        CopyTextCommand = new RelayCommand(_ =>
        {
            if (!IsNullOrEmpty(TranslatedText))
            {
                Clipboard.SetText(TranslatedText);
            }
        });

        ToggleOverlayVisibilityCommand = new RelayCommand(_ => IsOverlayVisible = !IsOverlayVisible,
            _ => !_settingsViewModel.SelectedArea.IsEmpty);

        RegisterHotkeys();

        _settingsViewModel.PropertyChanged += OnSettingsChanged;

        _settingsViewModel = settingsViewModel;

        InitializeMenu();

        CurrentView = this;

        NavigateCommand = new RelayCommand(obj =>
        {
            if (obj is NavigationItem item && item.DestinationViewModel != null)
            {
                CurrentView = item.DestinationViewModel;
            }
        });
    }

    private void OnResultReceived(object? sender, TranslationResultArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LastCapturedImage = e.Image;
            TranslatedText = e.TranslatedText;

            DetectedOcrBlocks.Clear();
            foreach (var block in e.Blocks) DetectedOcrBlocks.Add(block);
        });
    }


    private void RegisterHotkeys()
    {
        _hotkeyService.Register(ModifierKeys.Control, Key.G, () =>
        {
            if (ToggleTranslationCommand.CanExecute(null))
            {
                ToggleTranslationCommand.Execute(null);
            }
        });

        _hotkeyService.Register(ModifierKeys.Control, Key.Q, () => { System.Windows.Application.Current.Shutdown(); });

        _hotkeyService.Register(ModifierKeys.Control | ModifierKeys.Shift, Key.A, () =>
        {
            if (SelectAreaCommand.CanExecute(null))
            {
                SelectAreaCommand.Execute(null);
            }
        });

        _hotkeyService.Register(ModifierKeys.Control, Key.B, () =>
        {
            if (ToggleOverlayVisibilityCommand.CanExecute(null))
            {
                ToggleOverlayVisibilityCommand.Execute(null);
            }
        });

        var positionDelta = 25;
        _hotkeyService.Register(ModifierKeys.Control, Key.Left, () => _overlayService.MoveOverlayX(-positionDelta));

        _hotkeyService.Register(ModifierKeys.Control, Key.Right, () => _overlayService.MoveOverlayX(positionDelta));

        _hotkeyService.Register(ModifierKeys.Control, Key.Up, () => _overlayService.MoveOverlayY(-positionDelta));

        _hotkeyService.Register(ModifierKeys.Control, Key.Down, () => _overlayService.MoveOverlayY(positionDelta));

        var sizeDelta = 10;
        _hotkeyService.Register(ModifierKeys.Control | ModifierKeys.Shift, Key.Right,
            () => _overlayService.ResizeOverlayWidth(sizeDelta));

        _hotkeyService.Register(ModifierKeys.Control | ModifierKeys.Shift, Key.Left,
            () => _overlayService.ResizeOverlayWidth(-sizeDelta));

        _hotkeyService.Register(ModifierKeys.Control | ModifierKeys.Shift, Key.Down,
            () => _overlayService.ResizeOverlayHeight(sizeDelta));

        _hotkeyService.Register(ModifierKeys.Control | ModifierKeys.Shift, Key.Up,
            () => _overlayService.ResizeOverlayHeight(-sizeDelta));
    }

    private void InitializeMenu()
    {
        var homeItem = new NavigationItem
        {
            Title = "Главная",
            DestinationViewModel = this
        };

        var settingsItem = new NavigationItem
        {
            Title = "Настройки",
            DestinationViewModel = null
        };

        settingsItem.Children.Add(new NavigationItem
        {
            Title = "Общие",
            DestinationViewModel = _settingsViewModel
        });

        var ocrItem = new NavigationItem
        {
            Title = "Языки OCR",
            DestinationViewModel = null
        };

        ocrItem.Children.Add(new NavigationItem
        {
            Title = "Tesseract OCR",
            DestinationViewModel = new TesseractSettingsViewModelWrapper(_settingsViewModel)
        });

        settingsItem.Children.Add(ocrItem);

        MenuItems.Add(homeItem);
        MenuItems.Add(settingsItem);
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.SelectedArea))
        {
            (StartCommand as RelayCommand)?.CanExecute();
        }
    }

    private void OpenSettings()
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.ShowDialog();
    }

    private void Start()
    {
        _screenTranslatorEngine.Start();
        IsProcessRunning = true;
        StatusText = "Работает...";
    }

    private void Stop()
    {
        _screenTranslatorEngine.Stop();
        IsProcessRunning = false;
        StatusText = "Остановлено";
    }

    public void Dispose()
    {
        _settingsViewModel.PropertyChanged -= OnSettingsChanged;
    }
}