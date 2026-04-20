using System.Text;
using GhostLang.Core.Pipelines.Steps;
using GhostLang.Core.Pipelines.Steps.Audio;
using GhostLang.Core.Pipelines.Steps.Audio.Implementations;
using GhostLang.Core.Pipelines.Steps.Implementations;
using GhostLang.Core.Services;
using GhostLang.Core.Services.Erasure;
using GhostLang.Core.Services.Vad;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Asr;
using GhostLang.Core.Settings.Erasure;
using GhostLang.Core.Settings.Ocr;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Pipelines;

public class PipelineBuilder(
    IPipelineRegistry registry,
    IOcrEngineFactory ocrFactory,
    ITranslationCacheService cacheService,
    ITextErasureEngineFactory erasureEngineFactory,
    ITranslationEngineFactory translationEngineFactory,
    IAsrEngineFactory asrEngineFactory,
    ISileroVadEngine sileroVadEngine,
    ISileroVadModelManager sileroVadModelManager,
    MotionDetectionStep motionDetectionStep) : IPipelineBuilder
{
    public IReadOnlyList<PipelineStepInfo> DescribeImagePipeline(AppConfig config)
    {
        return registry.GetImagePipelineSteps()
            .OrderBy(s => s.Order)
            .Select(s => BuildStepInfo(s.Order, s.Name, s.IsOptional, s.StepId, GetImageEngineName(s.StepId, config), config))
            .ToList();
    }

    public IReadOnlyList<PipelineStepInfo> DescribeAudioPipeline(AppConfig config)
    {
        return registry.GetAudioPipelineSteps()
            .OrderBy(s => s.Order)
            .Select(s => BuildStepInfo(s.Order, s.Name, s.IsOptional, s.StepId, GetAudioEngineName(s.StepId, config), config))
            .ToList();
    }

    private static string? GetImageEngineName(string stepId, AppConfig config) => stepId switch
    {
        "step.image.ocr" => FormatEngine(config.ActiveOcrEngine),
        "step.image.text_erasure" => FormatEngine(config.ActiveErasureEngine),
        "step.image.translation" => FormatEngine(config.ActiveTranslationEngine),
        _ => null
    };

    private static string? GetAudioEngineName(string stepId, AppConfig config) => stepId switch
    {
        "step.audio.asr" => FormatEngine(config.ActiveAsrEngine),
        "step.audio.translation" => FormatEngine(config.ActiveTranslationEngine),
        _ => null
    };

    private static PipelineStepInfo BuildStepInfo(int order, string name, bool isOptional, string stepId,
        string? engine, AppConfig config)
    {
        var isActive = !isOptional ||
                       (config.OptionalStepStates.TryGetValue(stepId, out var state) && state);
        return new PipelineStepInfo(order, name, engine, !isOptional, isActive);
    }

    private static string? FormatEngine(object? options)
    {
        if (options == null) return null;
        var typeName = options.GetType().Name;
        return typeName.EndsWith("Options") ? typeName[..^"Options".Length] : typeName;
    }

    public IImageTranslationPipeline BuildImagePipeline(AppConfig config)
    {
        var steps = new List<IPipelineStep>();

        motionDetectionStep.IsEnabled = IsStepEnabled(config, "step.image.motion");
        steps.Add(motionDetectionStep);

        steps.Add(new ImagePreProcessStep(config.PreProcessOptions)
            { IsEnabled = IsStepEnabled(config, "step.image.preprocess") });

        if (config.ActiveOcrEngine != null)
        {
            steps.Add(new OcrStep(ocrFactory.Create(config.ActiveOcrEngine)));
        }

        var erasureOptions = config.ActiveErasureEngine;
        var erasureEngine = erasureEngineFactory.CreateEngine(erasureOptions);
        var textErasureStep = new TextErasureStep(erasureEngine);

        if (config.OptionalStepStates.TryGetValue("step.image.text_erasure", out var isErasureEnabled))
        {
            textErasureStep.IsEnabled = isErasureEnabled;
        }

        steps.Add(new TextErasureStep(erasureEngine) { IsEnabled = IsStepEnabled(config, "step.image.text_erasure") });

        cacheService.Configure(config.CacheTtlMinutes, config.CacheMaxCharacters);
        steps.Add(new TranslationCacheCheckStep(cacheService) { IsEnabled = IsStepEnabled(config, "step.image.cachecheck") });

        steps.Add(new GlossaryTokenizationStep(config.GlossaryRules, config.GlossaryTokenMode) { IsEnabled = IsStepEnabled(config, "step.image.glossary") });

        cacheService.SetEngineTag(config.ActiveTranslationEngine.GetType().Name);
        var translationEngine = translationEngineFactory.Create(config.ActiveTranslationEngine);
        steps.Add(new TranslationStep(translationEngine, cacheService));

        steps.Add(new GlossaryRestorationStep { IsEnabled = IsStepEnabled(config, "step.image.glossary_restore") });

        steps.Add(new TextRenderingStep(config.TextRendering));

        return new ImageTranslationPipeline(steps, config.TranslationDeduplicationEnabled);
    }

    public IAudioTranslationPipeline BuildAudioPipeline(AppConfig config)
    {
        var steps = new List<IAudioPipelineStep>
        {
            new VoiceActivityDetectionStep(config.VadOptions, sileroVadEngine, sileroVadModelManager)
                { IsEnabled = IsStepEnabled(config, "step.audio.vad") },
            new AudioPreProcessStep(config.AudioPreProcessOptions) { IsEnabled = IsStepEnabled(config, "step.audio.preprocess") }
        };

        if (config.ActiveAsrEngine != null)
        {
            steps.Add(new SpeechRecognitionStep(asrEngineFactory.Create(config.ActiveAsrEngine)));
        }

        cacheService.Configure(config.CacheTtlMinutes, config.CacheMaxCharacters);
        steps.Add(new AudioTranslationCacheCheckStep(cacheService) { IsEnabled = IsStepEnabled(config, "step.audio.cachecheck") });

        steps.Add(new AudioGlossaryTokenizationStep(config.GlossaryRules, config.GlossaryTokenMode) { IsEnabled = IsStepEnabled(config, "step.audio.glossary") });

        cacheService.SetEngineTag(config.ActiveTranslationEngine.GetType().Name);
        var translationEngine = translationEngineFactory.Create(config.ActiveTranslationEngine);
        steps.Add(new AudioTranslationStep(translationEngine, cacheService));

        steps.Add(new AudioGlossaryRestorationStep { IsEnabled = IsStepEnabled(config, "step.audio.glossary_restore") });

        steps.Add(new SubtitleRenderingStep());

        return new AudioTranslationPipeline(steps, config.TranslationDeduplicationEnabled);
    }

    private bool IsStepEnabled(AppConfig config, string stepId)
    {
        return config.OptionalStepStates.TryGetValue(stepId, out var state) && state;
    }
}
