using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Settings.Asr;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class VoskAsrSettingsViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty] private string _modelPath = "Models/Vosk/ru-small";

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            ModelPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void OpenDownloadPage()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://alphacephei.com/vosk/models",
            UseShellExecute = true
        });
    }

    public string EngineName => "Vosk (Local)";
    public Type OptionsType => typeof(VoskAsrOptions);

    public object GetOptions() => new VoskAsrOptions { ModelPath = ModelPath };

    public void ApplyOptions(object options)
    {
        if (options is not VoskAsrOptions opt) return;
        ModelPath = opt.ModelPath;
    }
}