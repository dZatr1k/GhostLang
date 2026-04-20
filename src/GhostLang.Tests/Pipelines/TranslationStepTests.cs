using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Steps.Implementations;
using GhostLang.Core.Services;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.Tests.Pipelines;

public class TranslationStepTests
{
    private const string BatchToken = "\u27EA\u27EB\u27EA\u27EB\u27EA\u27EB";

    [Fact]
    public async Task Batch_FragmentsContainingTripleDash_SplitsCorrectly()
    {

        var fragments = new List<TextFragment>
        {
            new() { OriginalText = "Chapter 1 --- The Beginning" },
            new() { OriginalText = "Hello world" },
            new() { OriginalText = "--- end ---" }
        };

        var engine = new EchoingBatchEngine();
        var cache = new NoOpCache();
        var step = new TranslationStep(engine, cache);

        var context = new TranslationContext { TextFragments = fragments };
        await step.ExecuteAsync(context);

        Assert.Equal("ru:Chapter 1 --- The Beginning", fragments[0].TranslatedText);
        Assert.Equal("ru:Hello world", fragments[1].TranslatedText);
        Assert.Equal("ru:--- end ---", fragments[2].TranslatedText);
    }

    [Fact]
    public async Task Batch_ErrorResponse_FallsThroughToParallel()
    {
        var fragments = new List<TextFragment>
        {
            new() { OriginalText = "foo" },
            new() { OriginalText = "bar" }
        };

        var engine = new ErrorOnBatchEngine();
        var cache = new NoOpCache();
        var step = new TranslationStep(engine, cache);

        var context = new TranslationContext { TextFragments = fragments };
        await step.ExecuteAsync(context);

        Assert.Equal("ru:foo", fragments[0].TranslatedText);
        Assert.Equal("ru:bar", fragments[1].TranslatedText);
    }

    [Fact]
    public async Task Duplicates_SingleEngineCall_ResultSpreadAcrossFragments()
    {

        var fragments = new List<TextFragment>
        {
            new() { OriginalText = "BAM!" },
            new() { OriginalText = "hello" },
            new() { OriginalText = "BAM!" },
            new() { OriginalText = "BAM!" }
        };

        var engine = new CountingEngine();
        var cache = new NoOpCache();
        var step = new TranslationStep(engine, cache);

        var context = new TranslationContext { TextFragments = fragments };
        await step.ExecuteAsync(context);

        Assert.Equal(1, engine.CallCount);
        Assert.Equal("ru:BAM!", fragments[0].TranslatedText);
        Assert.Equal("ru:hello", fragments[1].TranslatedText);
        Assert.Equal("ru:BAM!", fragments[2].TranslatedText);
        Assert.Equal("ru:BAM!", fragments[3].TranslatedText);
    }

    [Fact]
    public async Task Parallel_ErrorResponse_NotCached()
    {
        var fragments = new List<TextFragment> { new() { OriginalText = "fails" } };

        var engine = new AlwaysErrorEngine();
        var cache = new RecordingCache();
        var step = new TranslationStep(engine, cache);

        var context = new TranslationContext { TextFragments = fragments };
        await step.ExecuteAsync(context);

        Assert.StartsWith("[Error", fragments[0].TranslatedText);
        Assert.Empty(cache.Added);
    }

    private class EchoingBatchEngine : ITranslationEngine
    {
        public IReadOnlySet<SupportedLanguage> SupportedLanguages { get; } = new HashSet<SupportedLanguage> { SupportedLanguage.Russian };

        public Task<string> TranslateAsync(string text, SupportedLanguage _, List<SupportedLanguage> __)
        {

            var parts = text.Split([BatchToken], StringSplitOptions.None)
                .Select(p => $"ru:{p.Trim()}");
            return Task.FromResult(string.Join($"\n{BatchToken}\n", parts));
        }
    }

    private class ErrorOnBatchEngine : ITranslationEngine
    {
        public IReadOnlySet<SupportedLanguage> SupportedLanguages { get; } = new HashSet<SupportedLanguage> { SupportedLanguage.Russian };

        public Task<string> TranslateAsync(string text, SupportedLanguage _, List<SupportedLanguage> __)
        {
            if (text.Contains(BatchToken))
                return Task.FromResult("[Error] rate limited");
            return Task.FromResult($"ru:{text}");
        }
    }

    private class AlwaysErrorEngine : ITranslationEngine
    {
        public IReadOnlySet<SupportedLanguage> SupportedLanguages { get; } = new HashSet<SupportedLanguage> { SupportedLanguage.Russian };

        public Task<string> TranslateAsync(string text, SupportedLanguage _, List<SupportedLanguage> __) =>
            Task.FromResult("[Error] engine down");
    }

    private class CountingEngine : ITranslationEngine
    {
        public int CallCount { get; private set; }
        public IReadOnlySet<SupportedLanguage> SupportedLanguages { get; } = new HashSet<SupportedLanguage> { SupportedLanguage.Russian };

        public Task<string> TranslateAsync(string text, SupportedLanguage _, List<SupportedLanguage> __)
        {
            CallCount++;
            var parts = text.Split([BatchToken], StringSplitOptions.None)
                .Select(p => $"ru:{p.Trim()}");
            return Task.FromResult(string.Join($"\n{BatchToken}\n", parts));
        }
    }

    private class NoOpCache : ITranslationCacheService
    {
        public bool TryGetTranslation(string originalText, SupportedLanguage targetLanguage, out string? translatedText)
        { translatedText = null; return false; }
        public void AddTranslation(string originalText, string translatedText, SupportedLanguage targetLanguage) { }
        public void SetEngineTag(string engineTag) { }
        public void Configure(int ttlMinutes, int maxCharacters) { }
        public void ClearCache() { }
    }

    private class RecordingCache : ITranslationCacheService
    {
        public List<(string original, string translated)> Added { get; } = [];
        public bool TryGetTranslation(string originalText, SupportedLanguage targetLanguage, out string? translatedText)
        { translatedText = null; return false; }
        public void AddTranslation(string originalText, string translatedText, SupportedLanguage targetLanguage) =>
            Added.Add((originalText, translatedText));
        public void SetEngineTag(string engineTag) { }
        public void Configure(int ttlMinutes, int maxCharacters) { }
        public void ClearCache() { }
    }
}
