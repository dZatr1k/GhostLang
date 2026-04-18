using System.Diagnostics;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Steps.Audio;

namespace GhostLang.Core.Pipelines;

public class AudioTranslationPipeline(IEnumerable<IAudioPipelineStep> steps) : IAudioTranslationPipeline
{
    public async Task<AudioTranslationContext> ProcessAsync(
        byte[] audioPcm,
        int sampleRate,
        int channelCount,
        SupportedLanguage targetLanguage,
        List<SupportedLanguage> sourceLanguage,
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