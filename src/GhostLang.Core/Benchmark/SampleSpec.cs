using System.Text.Json;
using System.Text.Json.Serialization;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Benchmark;

public record SampleSpec
{
    public required string Directory { get; init; }
    public required string Name { get; init; }
    public required SupportedLanguage SourceLanguage { get; init; }
    public required SupportedLanguage TargetLanguage { get; init; }
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public string ExpectedTextPath => Path.Combine(Directory, "expected.txt");

    public string? ResolveRegionImagePath()
    {
        var canonical = Path.Combine(Directory, "region.png");
        if (File.Exists(canonical)) return canonical;

        var anyPng = System.IO.Directory.EnumerateFiles(Directory, "*.png").FirstOrDefault();
        return anyPng;
    }

    public string? ResolveAudioPath()
    {
        var canonical = Path.Combine(Directory, "audio.wav");
        if (File.Exists(canonical)) return canonical;

        string[] extensions = ["*.wav", "*.mp3", "*.m4a", "*.flac", "*.ogg"];
        foreach (var pattern in extensions)
        {
            var match = System.IO.Directory.EnumerateFiles(Directory, pattern).FirstOrDefault();
            if (match is not null) return match;
        }
        return null;
    }

    public static bool TryLoad(string sampleDirectory, out SampleSpec? spec, out string? error)
    {
        spec = null;
        error = null;

        var metaPath = Path.Combine(sampleDirectory, "meta.json");
        if (!File.Exists(metaPath))
        {
            error = $"meta.json not found in {sampleDirectory}";
            return false;
        }

        try
        {
            var json = File.ReadAllText(metaPath);
            var meta = JsonSerializer.Deserialize<SampleMeta>(json, JsonOptions);
            if (meta is null)
            {
                error = $"meta.json empty in {sampleDirectory}";
                return false;
            }

            spec = new SampleSpec
            {
                Directory = sampleDirectory,
                Name = string.IsNullOrWhiteSpace(meta.Name) ? Path.GetFileName(sampleDirectory) : meta.Name,
                SourceLanguage = ParseLanguage(meta.SourceLangs?.FirstOrDefault() ?? "English"),
                TargetLanguage = ParseLanguage(meta.TargetLang ?? "Russian"),
                Description = meta.Description ?? string.Empty,
                Tags = meta.Tags ?? Array.Empty<string>()
            };
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to parse meta.json in {sampleDirectory}: {ex.Message}";
            return false;
        }
    }

    private static SupportedLanguage ParseLanguage(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return SupportedLanguage.English;
        return Enum.TryParse<SupportedLanguage>(name, ignoreCase: true, out var lang)
            ? lang
            : SupportedLanguage.English;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private record SampleMeta
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("source_langs")] public string[]? SourceLangs { get; init; }
        [JsonPropertyName("target_lang")] public string? TargetLang { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
        [JsonPropertyName("tags")] public string[]? Tags { get; init; }
    }
}

public record BenchmarkProgress
{
    public required int Current { get; init; }
    public required int Total { get; init; }
    public required string SampleName { get; init; }
    public double FractionDone => Total == 0 ? 0.0 : (double)Current / Total;
}
