namespace GhostLang.Core.Services.Vad;

public class SileroVadModelManager : ISileroVadModelManager
{
    private static readonly HttpClient Http = new();

    private const string ModelUrl =
        "https://github.com/snakers4/silero-vad/raw/master/src/silero_vad/data/silero_vad.onnx";

    private readonly string _modelDirectory =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Silero");

    public string ModelFilePath => Path.Combine(_modelDirectory, "silero_vad.onnx");

    public bool IsModelDownloaded => File.Exists(ModelFilePath);

    public async Task DownloadAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_modelDirectory);

        using var response = await Http.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var canReportProgress = totalBytes > 0 && progress is not null;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(ModelFilePath, FileMode.Create,
            FileAccess.Write, FileShare.None, 8192, useAsync: true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) != 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            if (canReportProgress)
                progress!.Report(totalRead * 100.0 / totalBytes);
        }
    }

    public void Delete()
    {
        if (File.Exists(ModelFilePath)) File.Delete(ModelFilePath);
    }
}
