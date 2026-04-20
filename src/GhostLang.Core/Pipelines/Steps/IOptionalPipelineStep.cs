namespace GhostLang.Core.Pipelines.Steps;

public interface IOptionalPipelineStep : IPipelineStep
{
    bool IsEnabled { get; set; }
}
