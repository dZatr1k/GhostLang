namespace GhostLang.Core.Pipelines;

public abstract record PipelineStatus
{

    public sealed record Started : PipelineStatus;

    public sealed record Stopped : PipelineStatus;

    public sealed record Active : PipelineStatus;

    public sealed record Error(string Message, System.Exception? Exception = null) : PipelineStatus;

    public sealed record FrameEmpty : PipelineStatus;

    public sealed record FrameUnchanged(int FrameNumber, int StreakCount) : PipelineStatus;

    public sealed record FrameBaseline(int FrameNumber) : PipelineStatus;

    public sealed record MajorContentChanged(double ChangeRatio, int Streak) : PipelineStatus;

    public sealed record PossibleChange(double ChangeRatio) : PipelineStatus;

    public sealed record FrameProcessing(int SizeKb) : PipelineStatus;

    public sealed record FrameStale(int FrameNumber, long ElapsedMs) : PipelineStatus;

    public sealed record FrameProcessed(int FrameNumber, long ElapsedMs, int Fragments, int Rendered, bool RecordingMode) : PipelineStatus;

    public sealed record CaptureOverflow(long TotalDroppedMs) : PipelineStatus;
}
