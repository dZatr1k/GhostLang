namespace GhostLang.Core.Pipelines.Steps.Audio;

public interface IAudioPipelineStep
{
    Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default);
}
