using GhostLang.Core.Settings.Audio;

namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class AudioPreProcessStep(AudioPreProcessOptions options) : IOptionalAudioPipelineStep
{
    public bool IsEnabled { get; set; }

    public AudioPreProcessOptions Options => options;

    public Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || !IsEnabled)
            return Task.CompletedTask;

        return Task.CompletedTask;
    }
}