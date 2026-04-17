using Whisper.net.Ggml;

namespace GhostLang.Core.Services.Asr;

public class WhisperModelManager : IWhisperModelManager
{
    public async Task<string> EnsureModelAsync(string modelName, string modelsPath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(modelsPath, $"{modelName}.bin");

        if (File.Exists(fullPath))
            return fullPath;

        Directory.CreateDirectory(modelsPath);

        var ggmlType = MapModelName(modelName);

        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
        await using var fileStream = File.Create(fullPath);
        await modelStream.CopyToAsync(fileStream, ct);

        return fullPath;
    }

    private static GgmlType MapModelName(string name)
    {
        var normalized = name.ToLowerInvariant().Replace("ggml-", "");
        return normalized switch
        {
            "tiny" => GgmlType.Tiny,
            "base" => GgmlType.Base,
            "small" => GgmlType.Small,
            "medium" => GgmlType.Medium,
            "large" or "large-v1" => GgmlType.LargeV1,
            "large-v2" => GgmlType.LargeV2,
            "large-v3" => GgmlType.LargeV3,
            _ => GgmlType.Base
        };
    }
}