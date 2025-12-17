using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GhostLang.Application.Models;

public class TessdataModel : INotifyPropertyChanged
{
    public string LanguageName { get; set; }
    public string Code { get; set; }
    public string Type { get; set; }
    public string DownloadUrl { get; set; }
    
    private bool _isDownloaded;
    public bool IsDownloaded
    {
        get => _isDownloaded;
        set
        {
            if (_isDownloaded != value)
            {
                _isDownloaded = value;
                OnPropertyChanged();
            }
        }
    }
    
    private bool _isDownloading;
    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            if (_isDownloading != value)
            {
                _isDownloading = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}