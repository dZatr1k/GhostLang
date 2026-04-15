namespace GhostLang.Core.Pipelines.Descriptors;

public class EngineDescriptor
{
    public string EngineId { get; init; } = string.Empty;
        
    public string Name { get; init; } = string.Empty;
        
    public Type OptionsType { get; init; } = typeof(object);
}