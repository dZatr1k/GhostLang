using System.Diagnostics;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Steps.Audio;

namespace GhostLang.Core.Pipelines;

public class AudioTranslationPipeline(IEnumerable<IAudioPipelineStep> steps, bool translationDeduplicationEnabled = true) : IAudioTranslationPipeline
{
    private bool _disposed;

    public async Task<AudioTranslationContext> ProcessAsync(
        byte[] audioPcm,
        int sampleRate,
        int channelCount,
        SupportedLanguage targetLanguage,
        List<SupportedLanguage> sourceLanguage,
        long? captureStartMs = null,
        CancellationToken ct = default)
    {
        if (targetLanguage is SupportedLanguage.Unknown)
            return new AudioTranslationContext();

        var context = new AudioTranslationContext
        {
            OriginalAudio = audioPcm,
            ProcessedAudio = audioPcm,
            SampleRate = sampleRate,
            ChannelCount = channelCount,
            TargetLanguage = targetLanguage,
            SourceLanguage = sourceLanguage,
            CaptureStartMs = captureStartMs,
            TranslationDeduplicationEnabled = translationDeduplicationEnabled
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

        if (context.CaptureStartMs is long ts)
        {
            foreach (var fragment in context.AudioFragments)
                fragment.CaptureStartMs = ts;
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
