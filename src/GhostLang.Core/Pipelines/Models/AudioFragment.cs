namespace GhostLang.Core.Pipelines.Models;

public class AudioFragment
{
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;

    public long StartMs { get; set; }

    public long EndMs { get; set; }

    public long? CaptureStartMs { get; set; }

    public long? AbsoluteStartMs => CaptureStartMs.HasValue ? CaptureStartMs.Value + StartMs : null;

    public long? AbsoluteEndMs => CaptureStartMs.HasValue ? CaptureStartMs.Value + EndMs : null;

    public float Confidence { get; set; }

    public bool IsResolvedFromCache { get; set; } = false;

    public int? SpeakerId { get; set; }
}
