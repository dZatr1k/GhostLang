namespace GhostLang.Core.Services.Asr;

public interface IWhisperModelManager
{
    Task<string> EnsureModelAsync(string modelName, string modelsPath, CancellationToken ct = default);

    bool IsModelDownloaded(string modelName, string modelsPath);

    void DeleteModel(string modelName, string modelsPath);

    string GetModelFilePath(string modelName, string modelsPath);
}
