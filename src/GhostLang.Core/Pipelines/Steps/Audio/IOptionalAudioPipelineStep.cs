namespace GhostLang.Core.Pipelines.Steps.Audio;

public interface IOptionalAudioPipelineStep : IAudioPipelineStep
{
    bool IsEnabled { get; set; }
}
