using GhostLang.Core.Settings;

namespace GhostLang.Core.Pipelines;

public interface IPipelineBuilder
{
    IReadOnlyList<PipelineStepInfo> DescribeImagePipeline(AppConfig config);

    IReadOnlyList<PipelineStepInfo> DescribeAudioPipeline(AppConfig config);

    IImageTranslationPipeline BuildImagePipeline(AppConfig config);

    IAudioTranslationPipeline BuildAudioPipeline(AppConfig config);
}
