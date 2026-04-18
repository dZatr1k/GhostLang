using GhostLang.Core.Settings;

namespace GhostLang.Core.Pipelines;

public interface IPipelineBuilder
{
    string GetPipelineDescription(AppConfig config);

    IImageTranslationPipeline BuildImagePipeline(AppConfig config);

    IAudioTranslationPipeline BuildAudioPipeline(AppConfig config);
}