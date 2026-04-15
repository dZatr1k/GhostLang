namespace GhostLang.Core.Pipelines.Models;

public class TextFragment
{
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public BoundingBox Bounds { get; set; } = new();
    
    public bool IsResolvedFromCache { get; set; } = false;
    
    public string TextColorHex { get; set; } = "#FFFFFF";
    public byte[]? CleanedPatch { get; set; }
    public byte[]? RenderedPatch { get; set; }
}