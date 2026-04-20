namespace GhostLang.Core.Services.Vad;

public interface ISileroVadModelManager
{
    string ModelFilePath { get; }
    bool IsModelDownloaded { get; }

    Task DownloadAsync(IProgress<double>? progress = null, CancellationToken ct = default);
    void Delete();
}
