using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Settings.Erasure;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class OpenCvErasureViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty]
    private int _inpaintRadius = 3;

    [ObservableProperty]
    private bool _useTeleaAlgorithm = true;
    
    [ObservableProperty]
    private int _dilationIterations = 3;

    public void ApplyOptions(object options)
    {
        if (options is OpenCvErasureOptions opt)
        {
            InpaintRadius = opt.InpaintRadius;
            UseTeleaAlgorithm = opt.UseTeleaAlgorithm;
        }
    }

    public string EngineName => "OpenCV Inpainting";
    public Type OptionsType => typeof(OpenCvErasureOptions);

    public object GetOptions()
    {
        return new OpenCvErasureOptions
        {
            InpaintRadius = InpaintRadius,
            UseTeleaAlgorithm = UseTeleaAlgorithm
        };
    }
}