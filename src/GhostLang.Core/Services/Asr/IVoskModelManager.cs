namespace GhostLang.Core.Services.Asr;

public record VoskModelInfo(string Name, string FullPath, bool IsValid, string? InvalidReason);

public interface IVoskModelManager
{

    IReadOnlyList<VoskModelInfo> DiscoverModels(string rootPath);

    bool IsValidModelDirectory(string modelPath);
}
