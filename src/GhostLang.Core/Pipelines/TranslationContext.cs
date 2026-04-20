using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GhostLang.Core.Pipelines;

public class TranslationContext
{

    public byte[]? OriginalImage { get; init; }

    public List<SupportedLanguage> SourceLanguage { get; init; } = [];

    public SupportedLanguage TargetLanguage { get; init; } = SupportedLanguage.Unknown;

    public byte[]? ProcessedImage { get; set; }

    public bool IsAborted { get; set; }

    public double ScaleFactor { get; set; } = 1.0;

    public bool IsSmartErasureEnabled { get; set; }

    public GlossaryTokenMode GlossaryTokenMode { get; set; } = GlossaryTokenMode.Placeholder;

    public bool TranslationDeduplicationEnabled { get; init; } = true;

    public Image<Rgba32>? OriginalPixels { get; set; }

    public List<TextFragment> TextFragments { get; init; } = [];

    public Dictionary<string, string> GlossaryTokenMap { get; } = new();

    public List<StepMetric> Metrics { get; } = new();
}
