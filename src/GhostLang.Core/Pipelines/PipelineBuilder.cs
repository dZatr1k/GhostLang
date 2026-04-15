using System.Text;
using GhostLang.Core.Pipelines.Steps;
using GhostLang.Core.Pipelines.Steps.Implementations;
using GhostLang.Core.Services;
using GhostLang.Core.Services.Erasure;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Erasure;
using GhostLang.Core.Settings.Ocr;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Pipelines;

public class PipelineBuilder(
    IPipelineRegistry registry,
    IOcrEngineFactory ocrFactory,
    ITranslationCacheService cacheService,
    ITextErasureEngineFactory erasureEngineFactory,
    ITranslationEngineFactory translationEngineFactory) : IPipelineBuilder
{
    public string GetPipelineDescription(AppConfig config)
    {
        var steps = registry.GetImagePipelineSteps();

        return string.Join("\n", steps.Select(s =>
        {
            var status = "";
            if (s.IsOptional)
            {
                var isEnabled = config.OptionalStepStates.TryGetValue(s.StepId, out var state) && state;
                status = isEnabled ? "(Active)" : "(Disabled)";
            }

            return $"{s.Order}: {s.Name} {status}".Trim();
        }));
    }

    public IImageTranslationPipeline BuildImagePipeline(AppConfig config)
    {
        var steps = new List<IPipelineStep>();

        steps.Add(new MotionDetectionStep { IsEnabled = IsStepEnabled(config, "step.image.motion") });

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

        return new ImageTranslationPipeline(steps);
    }

    private bool IsStepEnabled(AppConfig config, string stepId)
    {
        return config.OptionalStepStates.TryGetValue(stepId, out var state) && state;
    }
}