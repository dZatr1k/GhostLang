using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Pipelines;

public class AudioTranslationContext
{
    public byte[]? OriginalAudio { get; set; }
    public byte[]? ProcessedAudio { get; set; }

    public int SampleRate { get; set; } = 16000;
    public int ChannelCount { get; set; } = 1;

    public List<AudioFragment> AudioFragments { get; set; } = [];

    public bool IsAborted { get; set; }

    public List<SupportedLanguage> SourceLanguage { get; set; } = [];
    public SupportedLanguage TargetLanguage { get; set; } = SupportedLanguage.Unknown;

    public Dictionary<string, string> GlossaryTokenMap { get; } = new();

    public GlossaryTokenMode GlossaryTokenMode { get; set; } = GlossaryTokenMode.Placeholder;

    public List<StepMetric> Metrics { get; } = new();
}