using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Pipelines;

public class AudioTranslationContext
{

    public int SampleRate { get; init; } = 16000;

    public int ChannelCount { get; init; } = 1;

    public List<SupportedLanguage> SourceLanguage { get; init; } = [];

    public SupportedLanguage TargetLanguage { get; init; } = SupportedLanguage.Unknown;

    public byte[]? OriginalAudio { get; set; }

    public byte[]? ProcessedAudio { get; set; }

    public long? CaptureStartMs { get; set; }

    public bool IsAborted { get; set; }

    public GlossaryTokenMode GlossaryTokenMode { get; set; } = GlossaryTokenMode.Placeholder;

    public bool TranslationDeduplicationEnabled { get; init; } = true;

    public List<AudioFragment> AudioFragments { get; init; } = [];

    public Dictionary<string, string> GlossaryTokenMap { get; } = new();

    public List<StepMetric> Metrics { get; } = new();
}
