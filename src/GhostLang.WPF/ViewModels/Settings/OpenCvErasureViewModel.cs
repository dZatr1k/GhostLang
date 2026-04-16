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

    [ObservableProperty]
    private int _adaptiveBlockSize = 15;

    [ObservableProperty]
    private double _adaptiveConstant = 10;

    public void ApplyOptions(object options)
    {
        if (options is OpenCvErasureOptions opt)
        {
            InpaintRadius = opt.InpaintRadius;
            UseTeleaAlgorithm = opt.UseTeleaAlgorithm;
            DilationIterations = opt.DilationIterations;
            AdaptiveBlockSize = opt.AdaptiveBlockSize;
            AdaptiveConstant = opt.AdaptiveConstant;
        }
    }

    public string EngineName => "OpenCV Inpainting";
    public Type OptionsType => typeof(OpenCvErasureOptions);

    public object GetOptions()
    {
        return new OpenCvErasureOptions
        {
            InpaintRadius = InpaintRadius,
            UseTeleaAlgorithm = UseTeleaAlgorithm,
            DilationIterations = DilationIterations,
            AdaptiveBlockSize = AdaptiveBlockSize,
            AdaptiveConstant = AdaptiveConstant
        };
    }
}