using GhostLang.Core.Services;

namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class AudioTranslationCacheCheckStep(ITranslationCacheService cacheService) : IOptionalAudioPipelineStep
{
    public bool IsEnabled { get; set; } = true;

    public Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || !IsEnabled || context.AudioFragments.Count == 0)
            return Task.CompletedTask;

        foreach (var fragment in context.AudioFragments)
        {
            if (fragment.IsResolvedFromCache) continue;

            if (cacheService.TryGetTranslation(fragment.OriginalText, context.TargetLanguage, out var translatedText) &&
                translatedText != null)
            {
                fragment.TranslatedText = translatedText;
                fragment.IsResolvedFromCache = true;
            }
        }

        return Task.CompletedTask;
    }
}
