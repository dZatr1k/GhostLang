using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Services.Ocr;

public interface ITesseractModelManager
{
    bool IsModelDownloaded(SupportedLanguage language, TesseractModelType modelType);

    string GetModelDirectoryPath(TesseractModelType modelType);

    Task DownloadModelAsync(SupportedLanguage language, TesseractModelType modelType, IProgress<double> progress, CancellationToken ct = default);
}