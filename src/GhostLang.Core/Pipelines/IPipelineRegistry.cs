using GhostLang.Core.Pipelines.Descriptors;

namespace GhostLang.Core.Pipelines;

public interface IPipelineRegistry
{
    IReadOnlyList<PipelineStepDescriptor> GetImagePipelineSteps();
}