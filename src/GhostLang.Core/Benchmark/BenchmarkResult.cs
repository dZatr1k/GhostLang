using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Benchmark;

public record BenchmarkResult
{
    public required string Name { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required PipelineKind Pipeline { get; init; }
    public required IReadOnlyList<SampleResult> Samples { get; init; }

    public double AverageCer => Samples.Count == 0 ? 0 : Samples.Average(s => s.CharacterErrorRate);
    public double AverageWer => Samples.Count == 0 ? 0 : Samples.Average(s => s.WordErrorRate);
    public double AverageBleu => Samples.Count == 0 ? 0 : Samples.Average(s => s.Bleu);
    public double AverageChrF => Samples.Count == 0 ? 0 : Samples.Average(s => s.ChrF);
    public long AverageLatencyMs => Samples.Count == 0 ? 0 : (long)Samples.Average(s => s.TotalLatencyMs);
    public int PassedCount => Samples.Count(s => !s.HasError);
    public int FailedCount => Samples.Count(s => s.HasError);
}

public enum PipelineKind
{
    Screen,
    Audio
}

public record SampleResult
{
    public required string SampleName { get; init; }
    public required long TotalLatencyMs { get; init; }
    public required double CharacterErrorRate { get; init; }
    public required double WordErrorRate { get; init; }
    public double Bleu { get; init; }
    public double ChrF { get; init; }
    public double AverageBoundIoU { get; init; }
    public int PredictedFragmentCount { get; init; }
    public int ExpectedFragmentCount { get; init; }
    public string? PredictedText { get; init; }
    public string? ExpectedText { get; init; }
    public IReadOnlyList<StepMetric> StepMetrics { get; init; } = Array.Empty<StepMetric>();
    public bool HasError { get; init; }
    public string? ErrorMessage { get; init; }
}
