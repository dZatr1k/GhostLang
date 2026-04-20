namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class SubtitleRenderingStep : IMandatoryAudioPipelineStep
{
    public Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
