namespace GhostLang.Core.Pipelines.Descriptors;

public class PipelineStepDescriptor
{
    public string StepId { get; init; } = string.Empty;
    public int Order { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NameKey { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DescriptionKey { get; init; } = string.Empty;
    public bool IsOptional { get; init; }
    public IReadOnlyList<EngineDescriptor> AvailableEngines { get; init; } = new List<EngineDescriptor>();
}
