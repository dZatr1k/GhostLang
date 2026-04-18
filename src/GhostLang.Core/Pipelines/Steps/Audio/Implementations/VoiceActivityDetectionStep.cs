using GhostLang.Core.Services.AudioCapture;
using GhostLang.Core.Settings.Audio;

namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class VoiceActivityDetectionStep(VadOptions options) : IOptionalAudioPipelineStep
{
    public bool IsEnabled { get; set; } = true;

    public Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || !IsEnabled || context.OriginalAudio is null || context.OriginalAudio.Length == 0)
            return Task.CompletedTask;

        var levelDb = AudioMath.ComputeLevelDb(context.OriginalAudio);
        if (levelDb < options.SilenceThresholdDb)
        {
            context.IsAborted = true;
        }

        return Task.CompletedTask;
    }
}