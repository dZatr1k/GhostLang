using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Settings.Erasure;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class SolidColorErasureViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty]
    private string _colorHex = "#000000";

    public void ApplyOptions(object options)
    {
        if (options is SolidColorErasureOptions opt)
        {
            ColorHex = opt.ColorHex;
        }
    }

    public string EngineName => "Solid Color Fill";
    public Type OptionsType => typeof(SolidColorErasureOptions);

    public object GetOptions()
    {
        return new SolidColorErasureOptions
        {
            ColorHex = ColorHex
        };
    }
}
