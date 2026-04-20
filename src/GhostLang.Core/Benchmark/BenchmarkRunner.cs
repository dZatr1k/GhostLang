using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Services;
using GhostLang.Core.Services.AudioCapture;
using GhostLang.Core.Settings;

namespace GhostLang.Core.Benchmark;

public class BenchmarkRunner(
    IPipelineBuilder pipelineBuilder,
    IConfigurationService configService)
{
    public async Task<BenchmarkResult> RunScreenAsync(
        string samplesRoot,
        IProgress<BenchmarkProgress>? progress = null,
        CancellationToken ct = default)
    {
        return await RunScreenWithConfigAsync(samplesRoot, CloneConfig(configService.Load()), "Screen benchmark", progress, ct);
    }

    public async Task<BenchmarkResult> RunAudioAsync(
        string samplesRoot,
        IProgress<BenchmarkProgress>? progress = null,
        CancellationToken ct = default)
    {
        return await RunAudioWithConfigAsync(samplesRoot, CloneConfig(configService.Load()), "Audio benchmark", progress, ct);
    }

    public async Task<List<BenchmarkResult>> RunScreenBatchAsync(
        string samplesRoot,
        IReadOnlyList<BenchmarkPreset> presets,
        IProgress<BenchmarkProgress>? progress = null,
        CancellationToken ct = default)
    {
        var baseConfig = configService.Load();
        var results = new List<BenchmarkResult>();
        for (var i = 0; i < presets.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var preset = presets[i];
            var cfg = CloneConfig(baseConfig);
            preset.Apply(cfg);

            var forwarded = progress is null ? null : new Progress<BenchmarkProgress>(p =>
            {
                progress.Report(new BenchmarkProgress
                {
                    Current = p.Current,
                    Total = p.Total,
                    SampleName = $"[{preset.Name}] {p.SampleName}"
                });
            });

            results.Add(await RunScreenWithConfigAsync(samplesRoot, cfg, preset.Name, forwarded, ct));
        }
        return results;
    }

    public async Task<List<BenchmarkResult>> RunAudioBatchAsync(
        string samplesRoot,
        IReadOnlyList<BenchmarkPreset> presets,
        IProgress<BenchmarkProgress>? progress = null,
        CancellationToken ct = default)
    {
        var baseConfig = configService.Load();
        var results = new List<BenchmarkResult>();
        for (var i = 0; i < presets.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var preset = presets[i];
            var cfg = CloneConfig(baseConfig);
            preset.Apply(cfg);

            var forwarded = progress is null ? null : new Progress<BenchmarkProgress>(p =>
            {
                progress.Report(new BenchmarkProgress
                {
                    Current = p.Current,
                    Total = p.Total,
                    SampleName = $"[{preset.Name}] {p.SampleName}"
                });
            });

            results.Add(await RunAudioWithConfigAsync(samplesRoot, cfg, preset.Name, forwarded, ct));
        }
        return results;
    }

    public async Task WarmUpScreenAsync(string samplesRoot, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var samplesDir = Path.Combine(samplesRoot, "screen");
        if (!Directory.Exists(samplesDir)) return;

        var firstDir = Directory.GetDirectories(samplesDir).OrderBy(p => p).FirstOrDefault();
        if (firstDir is null) return;

        if (!SampleSpec.TryLoad(firstDir, out var spec, out _) || spec is null) return;

        progress?.Report($"Warming up screen: {spec.Name}");
        var config = CloneConfig(configService.Load());
        await RunScreenSampleAsync(spec, config, ct);
    }

    public async Task WarmUpAudioAsync(string samplesRoot, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var samplesDir = Path.Combine(samplesRoot, "audio");
        if (!Directory.Exists(samplesDir)) return;

        var firstDir = Directory.GetDirectories(samplesDir).OrderBy(p => p).FirstOrDefault();
        if (firstDir is null) return;

        if (!SampleSpec.TryLoad(firstDir, out var spec, out _) || spec is null) return;

        progress?.Report($"Warming up audio: {spec.Name}");
        var config = CloneConfig(configService.Load());
        await RunAudioSampleAsync(spec, config, ct);
    }

    public SampleValidation ValidateSamples(string samplesRoot)
    {
        var screen = CollectValidation(Path.Combine(samplesRoot, "screen"), isScreen: true);
        var audio = CollectValidation(Path.Combine(samplesRoot, "audio"), isScreen: false);
        return new SampleValidation { Screen = screen, Audio = audio };
    }

    public static string ExportAggregateCsv(IEnumerable<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("preset,pipeline,sample,cer,wer,bleu,chrf,latency_ms,fragments,has_error,error");
        var ci = CultureInfo.InvariantCulture;
        foreach (var r in results)
        {
            foreach (var s in r.Samples)
            {
                sb.Append(Csv(r.Name)).Append(',')
                  .Append(r.Pipeline).Append(',')
                  .Append(Csv(s.SampleName)).Append(',')
                  .Append(s.CharacterErrorRate.ToString("F4", ci)).Append(',')
                  .Append(s.WordErrorRate.ToString("F4", ci)).Append(',')
                  .Append(s.Bleu.ToString("F4", ci)).Append(',')
                  .Append(s.ChrF.ToString("F4", ci)).Append(',')
                  .Append(s.TotalLatencyMs).Append(',')
                  .Append(s.PredictedFragmentCount).Append(',')
                  .Append(s.HasError ? "1" : "0").Append(',')
                  .Append(Csv(s.ErrorMessage ?? string.Empty))
                  .AppendLine();
            }
        }
        return sb.ToString();
    }

    private async Task<BenchmarkResult> RunScreenWithConfigAsync(
        string samplesRoot,
        AppConfig config,
        string runName,
        IProgress<BenchmarkProgress>? progress,
        CancellationToken ct)
    {
        var samplesDir = Path.Combine(samplesRoot, "screen");
        var sampleDirs = Directory.Exists(samplesDir)
            ? Directory.GetDirectories(samplesDir).OrderBy(p => p).ToArray()
            : Array.Empty<string>();

        var results = new List<SampleResult>();
        for (var i = 0; i < sampleDirs.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (!SampleSpec.TryLoad(sampleDirs[i], out var spec, out var loadError) || spec is null)
            {
                results.Add(ErrorResult(Path.GetFileName(sampleDirs[i]), loadError ?? "Unknown error"));
                progress?.Report(new BenchmarkProgress { Current = i + 1, Total = sampleDirs.Length, SampleName = Path.GetFileName(sampleDirs[i]) });
                continue;
            }

            progress?.Report(new BenchmarkProgress { Current = i, Total = sampleDirs.Length, SampleName = spec.Name });
            results.Add(await RunScreenSampleAsync(spec, config, ct));
            progress?.Report(new BenchmarkProgress { Current = i + 1, Total = sampleDirs.Length, SampleName = spec.Name });
        }

        return new BenchmarkResult
        {
            Name = runName,
            TimestampUtc = DateTime.UtcNow,
            Pipeline = PipelineKind.Screen,
            Samples = results
        };
    }

    private async Task<BenchmarkResult> RunAudioWithConfigAsync(
        string samplesRoot,
        AppConfig config,
        string runName,
        IProgress<BenchmarkProgress>? progress,
        CancellationToken ct)
    {
        var samplesDir = Path.Combine(samplesRoot, "audio");
        var sampleDirs = Directory.Exists(samplesDir)
            ? Directory.GetDirectories(samplesDir).OrderBy(p => p).ToArray()
            : Array.Empty<string>();

        var results = new List<SampleResult>();
        for (var i = 0; i < sampleDirs.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (!SampleSpec.TryLoad(sampleDirs[i], out var spec, out var loadError) || spec is null)
            {
                results.Add(ErrorResult(Path.GetFileName(sampleDirs[i]), loadError ?? "Unknown error"));
                progress?.Report(new BenchmarkProgress { Current = i + 1, Total = sampleDirs.Length, SampleName = Path.GetFileName(sampleDirs[i]) });
                continue;
            }

            progress?.Report(new BenchmarkProgress { Current = i, Total = sampleDirs.Length, SampleName = spec.Name });
            results.Add(await RunAudioSampleAsync(spec, config, ct));
            progress?.Report(new BenchmarkProgress { Current = i + 1, Total = sampleDirs.Length, SampleName = spec.Name });
        }

        return new BenchmarkResult
        {
            Name = runName,
            TimestampUtc = DateTime.UtcNow,
            Pipeline = PipelineKind.Audio,
            Samples = results
        };
    }

    private async Task<SampleResult> RunScreenSampleAsync(SampleSpec spec, AppConfig config, CancellationToken ct)
    {
        try
        {
            var imagePath = spec.ResolveRegionImagePath();
            if (imagePath is null)
                return ErrorResult(spec.Name, $"no .png image found in {spec.Directory}");
            if (!File.Exists(spec.ExpectedTextPath))
                return ErrorResult(spec.Name, $"expected.txt not found in {spec.Directory}");

            var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
            var expectedText = (await File.ReadAllTextAsync(spec.ExpectedTextPath, ct)).Trim();

            using var pipeline = pipelineBuilder.BuildImagePipeline(config);
            var sw = Stopwatch.StartNew();
            var context = await pipeline.ProcessFrameAsync(
                imageBytes,
                spec.TargetLanguage,
                new List<Core.Pipelines.Enums.SupportedLanguage> { spec.SourceLanguage });
            sw.Stop();

            var predictedText = context.TextFragments is null
                ? string.Empty
                : string.Join("\n", context.TextFragments.Select(f => f.OriginalText ?? string.Empty)).Trim();

            return new SampleResult
            {
                SampleName = spec.Name,
                TotalLatencyMs = sw.ElapsedMilliseconds,
                CharacterErrorRate = Metrics.CharacterErrorRate(predictedText, expectedText),
                WordErrorRate = Metrics.WordErrorRate(predictedText, expectedText),
                Bleu = Metrics.Bleu4(predictedText, expectedText),
                ChrF = Metrics.ChrF(predictedText, expectedText),
                PredictedFragmentCount = context.TextFragments?.Count ?? 0,
                PredictedText = predictedText,
                ExpectedText = expectedText,
                StepMetrics = context.Metrics.ToArray()
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ErrorResult(spec.Name, ex.Message);
        }
    }

    private async Task<SampleResult> RunAudioSampleAsync(SampleSpec spec, AppConfig config, CancellationToken ct)
    {
        try
        {
            var audioPath = spec.ResolveAudioPath();
            if (audioPath is null)
                return ErrorResult(spec.Name, $"no audio file found in {spec.Directory}");
            if (!File.Exists(spec.ExpectedTextPath))
                return ErrorResult(spec.Name, $"expected.txt not found in {spec.Directory}");

            var pcm = await Task.Run(
                () => AudioFileLoader.LoadAsPcm16Mono16kHz(audioPath),
                ct);
            var expectedText = (await File.ReadAllTextAsync(spec.ExpectedTextPath, ct)).Trim();

            using var pipeline = pipelineBuilder.BuildAudioPipeline(config);
            var sw = Stopwatch.StartNew();
            var context = await pipeline.ProcessAsync(
                pcm,
                AudioFileLoader.TargetSampleRate,
                AudioFileLoader.TargetChannels,
                spec.TargetLanguage,
                new List<Core.Pipelines.Enums.SupportedLanguage> { spec.SourceLanguage });
            sw.Stop();

            var predictedText = string.Join(" ", context.AudioFragments
                .Select(f => f.OriginalText ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t))).Trim();

            return new SampleResult
            {
                SampleName = spec.Name,
                TotalLatencyMs = sw.ElapsedMilliseconds,
                CharacterErrorRate = Metrics.CharacterErrorRate(predictedText, expectedText),
                WordErrorRate = Metrics.WordErrorRate(predictedText, expectedText),
                Bleu = Metrics.Bleu4(predictedText, expectedText),
                ChrF = Metrics.ChrF(predictedText, expectedText),
                PredictedFragmentCount = context.AudioFragments.Count,
                PredictedText = predictedText,
                ExpectedText = expectedText,
                StepMetrics = context.Metrics.ToArray()
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ErrorResult(spec.Name, ex.Message);
        }
    }

    private static List<SampleValidationEntry> CollectValidation(string dir, bool isScreen)
    {
        var entries = new List<SampleValidationEntry>();
        if (!Directory.Exists(dir)) return entries;

        foreach (var sub in Directory.GetDirectories(dir).OrderBy(p => p))
        {
            var name = Path.GetFileName(sub);
            var issues = new List<string>();

            if (!SampleSpec.TryLoad(sub, out var spec, out var loadError) || spec is null)
            {
                issues.Add(loadError ?? "invalid meta.json");
                entries.Add(new SampleValidationEntry { Name = name, IsValid = false, Issues = issues });
                continue;
            }

            if (!File.Exists(spec.ExpectedTextPath))
                issues.Add("missing expected.txt");

            if (isScreen)
            {
                if (spec.ResolveRegionImagePath() is null)
                    issues.Add("missing *.png");
            }
            else
            {
                if (spec.ResolveAudioPath() is null)
                    issues.Add("missing audio file (wav/mp3/m4a/flac/ogg)");
            }

            entries.Add(new SampleValidationEntry
            {
                Name = spec.Name,
                IsValid = issues.Count == 0,
                Issues = issues
            });
        }
        return entries;
    }

    private static SampleResult ErrorResult(string name, string message) => new()
    {
        SampleName = name,
        TotalLatencyMs = 0,
        CharacterErrorRate = 1.0,
        WordErrorRate = 1.0,
        Bleu = 0.0,
        ChrF = 0.0,
        HasError = true,
        ErrorMessage = message
    };

    public static string SerializeToJson(BenchmarkResult result)
    {
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    public static BenchmarkResult? DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<BenchmarkResult>(json, JsonOptions);
    }

    private static AppConfig CloneConfig(AppConfig source)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needsQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuote) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };
}

public record SampleValidation
{
    public required IReadOnlyList<SampleValidationEntry> Screen { get; init; }
    public required IReadOnlyList<SampleValidationEntry> Audio { get; init; }

    public int TotalCount => Screen.Count + Audio.Count;
    public int ValidCount => Screen.Count(e => e.IsValid) + Audio.Count(e => e.IsValid);
    public int InvalidCount => TotalCount - ValidCount;
}

public record SampleValidationEntry
{
    public required string Name { get; init; }
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Issues { get; init; }
}
