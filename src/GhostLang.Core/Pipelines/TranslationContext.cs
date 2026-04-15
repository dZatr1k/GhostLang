using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Pipelines;

public class TranslationContext
{
    public byte[]? OriginalImage { get; set; }
    public byte[]? ProcessedImage { get; set; }
    
    public List<TextFragment> TextFragments { get; set; } = [];

    public bool IsAborted { get; set; }
    
    public List<SupportedLanguage> SourceLanguage { get; set; } = [];
    public SupportedLanguage TargetLanguage { get; set; } = SupportedLanguage.Unknown;
    
    public double ScaleFactor { get; set; } = 1.0;

    public bool IsSmartErasureEnabled { get; set; }

    public Dictionary<string, string> GlossaryTokenMap { get; } = new();

    public GlossaryTokenMode GlossaryTokenMode { get; set; } = GlossaryTokenMode.Placeholder;

    public List<StepMetric> Metrics { get; } = new();
}