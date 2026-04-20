namespace GhostLang.Core.Pipelines;

public record PipelineStepInfo(
    int Order,
    string Name,
    string? Engine,
    bool IsMandatory,
    bool IsActive)
{
    public bool HasEngine => !string.IsNullOrWhiteSpace(Engine);
}
