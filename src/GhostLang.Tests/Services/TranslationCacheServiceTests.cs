using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services;

namespace GhostLang.Tests.Services;

public class TranslationCacheServiceTests
{
    [Fact]
    public void NormalizedKey_CaseAndWhitespaceVariants_SameEntry()
    {

        var cache = new TranslationCacheService();
        cache.Configure(ttlMinutes: 60, maxCharacters: 10000);

        cache.AddTranslation("Hello World", "Привет мир", SupportedLanguage.Russian);

        Assert.True(cache.TryGetTranslation("hello world", SupportedLanguage.Russian, out var a));
        Assert.Equal("Привет мир", a);

        Assert.True(cache.TryGetTranslation("Hello  World", SupportedLanguage.Russian, out var b));
        Assert.Equal("Привет мир", b);

        Assert.True(cache.TryGetTranslation("  HELLO WORLD  ", SupportedLanguage.Russian, out var c));
        Assert.Equal("Привет мир", c);

        Assert.True(cache.TryGetTranslation("hello\nworld", SupportedLanguage.Russian, out var d));
        Assert.Equal("Привет мир", d);

        Assert.True(cache.TryGetTranslation("hello\tworld", SupportedLanguage.Russian, out var e));
        Assert.Equal("Привет мир", e);
    }

    [Fact]
    public void DifferentTargetLanguages_DoNotCollide()
    {
        var cache = new TranslationCacheService();
        cache.Configure(ttlMinutes: 60, maxCharacters: 10000);

        cache.AddTranslation("hello", "привет", SupportedLanguage.Russian);
        cache.AddTranslation("hello", "hola", SupportedLanguage.Spanish);

        Assert.True(cache.TryGetTranslation("hello", SupportedLanguage.Russian, out var ru));
        Assert.Equal("привет", ru);

        Assert.True(cache.TryGetTranslation("hello", SupportedLanguage.Spanish, out var es));
        Assert.Equal("hola", es);
    }

    [Fact]
    public void DifferentContent_DoNotCollide()
    {
        var cache = new TranslationCacheService();
        cache.Configure(ttlMinutes: 60, maxCharacters: 10000);

        cache.AddTranslation("cat", "кот", SupportedLanguage.Russian);
        cache.AddTranslation("dog", "собака", SupportedLanguage.Russian);

        Assert.True(cache.TryGetTranslation("cat", SupportedLanguage.Russian, out var cat));
        Assert.Equal("кот", cat);

        Assert.True(cache.TryGetTranslation("dog", SupportedLanguage.Russian, out var dog));
        Assert.Equal("собака", dog);
    }
}
