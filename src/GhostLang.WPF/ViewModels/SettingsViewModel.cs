using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;
using GhostLang.WPF.Commands;

namespace GhostLang.WPF.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ITessdataService _tessdataService;

    private int _timerIntervalMilliseconds = 1000;
    public int TimerIntervalMilliseconds
    {
        get => _timerIntervalMilliseconds;
        set => SetField(ref _timerIntervalMilliseconds, value);
    }
    
    private Rect _selectedArea;
    public Rect SelectedArea
    {
        get => _selectedArea;
        set => SetField(ref _selectedArea, value);
    }
    
    public ObservableCollection<TessdataModel> TessdataList { get; set; }
    public ICommand DownloadLanguageCommand { get; }

    public SettingsViewModel(ITessdataService tessdataService)
    {
        _tessdataService = tessdataService;
        
        var list = _tessdataService.GetAvailableLanguages();
        TessdataList = new ObservableCollection<TessdataModel>(list);
        
        DownloadLanguageCommand = new RelayCommand(
            execute: async void (param) =>
            {
                try
                {
                    await DownloadLanguage((TessdataModel)param!);
                }
                catch (Exception e)
                {
                    throw;
                }
            },
            canExecute: param => 
            {
                if (param is TessdataModel model)
                    return !model.IsDownloading;
                return false;
            }
        );
    }

    private async Task DownloadLanguage(TessdataModel model)
    {
        try
        {
            model.IsDownloading = true;
            (DownloadLanguageCommand as RelayCommand)?.CanExecute(null);

            await _tessdataService.DownloadLanguageAsync(model);
            
            model.IsDownloaded = true;
            
            UpdateListStatus(model.Code);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}");
        }
        finally
        {
            model.IsDownloading = false;
            (DownloadLanguageCommand as RelayCommand)?.CanExecute(null);
        }
    }
    
    private void UpdateListStatus(string code)
    {
        foreach (var item in TessdataList)
        {
            if (item.Code == code)
            {
                item.IsDownloaded = true;
            }
        }
    }
}