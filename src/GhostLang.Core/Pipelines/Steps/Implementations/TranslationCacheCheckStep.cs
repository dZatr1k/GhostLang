using GhostLang.Core.Services;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class TranslationCacheCheckStep(ITranslationCacheService cacheService) : IOptionalPipelineStep
{
    public string StepName => "Translation Cache";

    public bool IsEnabled { get; set; } = true;

    public Task ExecuteAsync(TranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || !IsEnabled || context.TextFragments.Count == 0)
            return Task.CompletedTask;

        foreach (var fragment in context.TextFragments)
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
