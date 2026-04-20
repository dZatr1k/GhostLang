using System.Diagnostics;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Steps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GhostLang.Core.Pipelines;

public class ImageTranslationPipeline(IEnumerable<IPipelineStep> steps, bool translationDeduplicationEnabled = true) : IImageTranslationPipeline
{
    private bool _disposed;

    public async Task<TranslationContext> ProcessFrameAsync(byte[] imageBytes, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguage, CancellationToken ct = default)
    {
        if (targetLanguage is SupportedLanguage.Unknown)
            return new TranslationContext();

        Image<Rgba32>? originalPixels = null;
        try
        {
            originalPixels = Image.Load<Rgba32>(imageBytes);
        }
        catch
        {

        }

        var context = new TranslationContext
        {
            OriginalImage = imageBytes,
            ProcessedImage = imageBytes,
            TargetLanguage = targetLanguage,
            SourceLanguage = sourceLanguage,
            OriginalPixels = originalPixels,
            TranslationDeduplicationEnabled = translationDeduplicationEnabled
        };

        try
        {
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
        }
        finally
        {

            context.OriginalPixels = null;
            originalPixels?.Dispose();
        }

        return context;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var step in steps)
        {
            if (step is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
