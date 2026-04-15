using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Settings.Ocr;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class WindowsOcrViewModel : ObservableObject, IEngineSettingsViewModel
{
    public string EngineName => "Windows OCR (Built-in)";

    public Type OptionsType => typeof(WindowsOcrOptions);

    public object GetOptions()
    {
        return new WindowsOcrOptions();
    }

    public void ApplyOptions(object options)
    {
    }
}