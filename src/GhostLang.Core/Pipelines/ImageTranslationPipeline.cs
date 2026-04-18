using System.Diagnostics;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Steps;

namespace GhostLang.Core.Pipelines;

public class ImageTranslationPipeline(IEnumerable<IPipelineStep> steps) : IImageTranslationPipeline
{
    public async Task<TranslationContext> ProcessFrameAsync(byte[] imageBytes, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguage, CancellationToken ct = default)
    {
        if (targetLanguage is SupportedLanguage.Unknown)
            return new TranslationContext();

        var context = new TranslationContext
        {
            OriginalImage = imageBytes,
            ProcessedImage = imageBytes,
            TargetLanguage = targetLanguage,
            SourceLanguage = sourceLanguage
        };

        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();

            await step.ExecuteAsync(context, ct);

            stopwatch.Stop();

            context.Metrics.Add(new StepMetric
            {
                StepName = step.GetType().Name,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            });

            if (context.IsAborted)
                break;
        }

        return context;
    }
}