using GhostLang.Core.Services;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class TranslationStep(ITranslationEngine translationEngine, ITranslationCacheService cacheService) : IMandatoryPipelineStep
{

    private const string BatchSeparatorToken = "\u27EA\u27EB\u27EA\u27EB\u27EA\u27EB";
    private const string BatchSeparator = "\n" + BatchSeparatorToken + "\n";

    public string StepName => "Machine Translation";

    public async Task ExecuteAsync(TranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted) return;

        var toTranslate = context.TextFragments
            .Where(f => !f.IsResolvedFromCache && !string.IsNullOrWhiteSpace(f.OriginalText))
            .ToList();

        if (toTranslate.Count == 0) return;

        if (context.SourceLanguage is { Count: 1 } &&
            context.SourceLanguage[0] == context.TargetLanguage)
        {
            foreach (var fragment in toTranslate)
                fragment.TranslatedText = fragment.OriginalText;
            return;
        }

        List<IGrouping<string, Models.TextFragment>> groups;
        List<Models.TextFragment> representatives;

        if (context.TranslationDeduplicationEnabled)
        {
            groups = toTranslate.GroupBy(f => f.OriginalText).ToList();
            representatives = groups.Select(g => g.First()).ToList();
        }
        else
        {
            groups = new List<IGrouping<string, Models.TextFragment>>();
            representatives = toTranslate;
        }

        if (representatives.Count > 1)
        {
            var batchSuccess = await TryBatchTranslate(representatives, context);
            if (batchSuccess)
            {
                SpreadToDuplicates(groups);
                return;
            }
        }

        await TranslateParallel(representatives, context);
        SpreadToDuplicates(groups);
    }

    private static void SpreadToDuplicates(IEnumerable<IGrouping<string, Models.TextFragment>> groups)
    {
        foreach (var g in groups)
        {
            var representative = g.First();
            foreach (var duplicate in g.Skip(1))
                duplicate.TranslatedText = representative.TranslatedText;
        }
    }

    private async Task<bool> TryBatchTranslate(
        List<Models.TextFragment> fragments, TranslationContext context)
    {
        try
        {
            var batchText = string.Join(BatchSeparator, fragments.Select(f => f.OriginalText));

            var batchResult = await translationEngine.TranslateAsync(
                batchText, context.TargetLanguage, context.SourceLanguage);

            if (IsEngineErrorResponse(batchResult))
                return false;

            var parts = batchResult.Split([BatchSeparatorToken], StringSplitOptions.None)
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

    private async Task TranslateParallel(
        List<Models.TextFragment> fragments, TranslationContext context)
    {
        var tasks = fragments.Select(async fragment =>
        {
            var translated = await translationEngine.TranslateAsync(
                fragment.OriginalText, context.TargetLanguage, context.SourceLanguage);

            fragment.TranslatedText = translated;

            if (!IsEngineErrorResponse(translated))
                cacheService.AddTranslation(fragment.OriginalText, translated, context.TargetLanguage);
        });

        await Task.WhenAll(tasks);
    }

    private static bool IsEngineErrorResponse(string? text) =>
        string.IsNullOrWhiteSpace(text) || text.StartsWith("[Error") || text.StartsWith("[Ошибка");
}
