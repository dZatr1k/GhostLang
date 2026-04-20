using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Utilities;

namespace GhostLang.Core.Services.Ocr;

public class TesseractModelManager : ITesseractModelManager
{
    private static readonly HttpClient HttpClient = new();

    private readonly string _baseModelsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Tesseract");

    public bool IsModelDownloaded(SupportedLanguage language, TesseractModelType modelType)
    {
        var directory = GetModelDirectoryPath(modelType);
        var langCode = language.ToTesseractCode();
        var filePath = Path.Combine(directory, $"{langCode}.traineddata");

        return File.Exists(filePath);
    }

    public string GetModelDirectoryPath(TesseractModelType modelType)
    {
        return Path.Combine(_baseModelsPath, modelType.ToString());
    }

    public async Task DownloadModelAsync(SupportedLanguage language, TesseractModelType modelType,
        IProgress<double> progress, CancellationToken ct = default)
    {
        var langCode = language.ToTesseractCode();
        var url = GetDownloadUrl(langCode, modelType);
        var directory = GetModelDirectoryPath(modelType);
        var filePath = Path.Combine(directory, $"{langCode}.traineddata");

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var canReportProgress = totalBytes != -1 && progress != null;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) != 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
            totalRead += bytesRead;

            if (canReportProgress)
            {
                progress?.Report((double)totalRead / totalBytes * 100);
            }
        }
    }

    public void DeleteModel(SupportedLanguage language, TesseractModelType modelType)
    {
        var directory = GetModelDirectoryPath(modelType);
        var langCode = language.ToTesseractCode();
        var filePath = Path.Combine(directory, $"{langCode}.traineddata");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetDownloadUrl(string langCode, TesseractModelType modelType)
    {
        return modelType switch
        {
            TesseractModelType.Fast =>
                $"https://github.com/tesseract-ocr/tessdata_fast/raw/refs/heads/main/{langCode}.traineddata",
            TesseractModelType.Best =>
                $"https://github.com/tesseract-ocr/tessdata/raw/refs/heads/main/{langCode}.traineddata",
            _ => throw new ArgumentOutOfRangeException(nameof(modelType))
        };
    }
}
