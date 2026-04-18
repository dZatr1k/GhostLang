using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Services;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class AudioTranslationStep(ITranslationEngine translationEngine, ITranslationCacheService cacheService) : IMandatoryAudioPipelineStep
{
    private const string BatchSeparator = "\n---\n";

    public async Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted) return;

        var toTranslate = context.AudioFragments
            .Where(f => !f.IsResolvedFromCache && !string.IsNullOrWhiteSpace(f.OriginalText))
            .ToList();

        if (toTranslate.Count == 0) return;

        if (toTranslate.Count > 1)
        {
            var batchSuccess = await TryBatchTranslate(toTranslate, context);
            if (batchSuccess) return;
        }

        await TranslateParallel(toTranslate, context);
    }

    private async Task<bool> TryBatchTranslate(List<AudioFragment> fragments, AudioTranslationContext context)
    {
        try
        {
            var batchText = string.Join(BatchSeparator, fragments.Select(f => f.OriginalText));

            var batchResult = await translationEngine.TranslateAsync(
                batchText, context.TargetLanguage, context.SourceLanguage);

            if (string.IsNullOrWhiteSpace(batchResult) || batchResult.StartsWith("[Error") || batchResult.StartsWith("[Ошибка"))
                return false;

            var parts = batchResult.Split(["---"], StringSplitOptions.None)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (parts.Count != fragments.Count)
                return false;

            for (var i = 0; i < fragments.Count; i++)
            {
                fragments[i].TranslatedText = parts[i];
                cacheService.AddTranslation(fragments[i].OriginalText, parts[i], context.TargetLanguage);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task TranslateParallel(List<AudioFragment> fragments, AudioTranslationContext context)
    {
        var tasks = fragments.Select(async fragment =>
        {
            fragment.TranslatedText = await translationEngine.TranslateAsync(
                fragment.OriginalText, context.TargetLanguage, context.SourceLanguage);

            cacheService.AddTranslation(fragment.OriginalText, fragment.TranslatedText, context.TargetLanguage);
        });

        await Task.WhenAll(tasks);
    }
}