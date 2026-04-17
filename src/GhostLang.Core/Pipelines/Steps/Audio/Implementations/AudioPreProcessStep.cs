namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class AudioPreProcessStep : IOptionalAudioPipelineStep
{
    public bool IsEnabled { get; set; }

    public Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}