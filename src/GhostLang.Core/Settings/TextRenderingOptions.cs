using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Settings;

public class TextRenderingOptions
{
    public string SelectedFontFamily { get; set; } = "Arial";

    public TextRenderingMode RenderingMode { get; set; } = TextRenderingMode.Compress;

    public bool UseOriginalColor { get; set; } = true;

    public string DefaultColorHex { get; set; } = "#FFFF00";
}