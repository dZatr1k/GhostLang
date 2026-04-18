namespace GhostLang.Core.Pipelines.Steps;

public interface IPipelineStep
{
    Task ExecuteAsync(TranslationContext context, CancellationToken ct = default);
}