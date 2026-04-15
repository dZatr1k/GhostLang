namespace GhostLang.Core.Settings.ImagePreProcessing;

public class ImagePreProcessOptions
{
    public FilterOption Upscale { get; set; } = new() { Value = 2.0f };
    public FilterOption GaussianBlur { get; set; } = new() { Value = 1.5f };
    public FilterOption Grayscale { get; set; } = new() { Value = 0f };
    public FilterOption Contrast { get; set; } = new() { Value = 1.5f };
    public FilterOption Binarize { get; set; } = new() { Value = 0.5f };
    public FilterOption Invert { get; set; } = new() { Value = 0f };
    
    public FilterOption Sharpen { get; set; } = new() { IsEnabled = false, Value = 1.0f };

    public FilterOption AutoLevel { get; set; } = new() { IsEnabled = false };

    public FilterOption Brightness { get; set; } = new() { IsEnabled = false, Value = 1.0f };
}