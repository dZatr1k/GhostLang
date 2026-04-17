namespace GhostLang.Core.Services.Asr;

public interface IWhisperModelManager
{
    Task<string> EnsureModelAsync(string modelName, string modelsPath, CancellationToken ct = default);
}