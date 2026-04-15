using System.Collections.Concurrent;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Services;

public class TranslationCacheService : ITranslationCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private string _currentEngineTag = string.Empty;
    private TimeSpan _ttl = TimeSpan.FromMinutes(60);
    private int _maxCharacters = 10000;
    private int _currentCharacters;

    public bool TryGetTranslation(string originalText, SupportedLanguage targetLanguage, out string? translatedText)
    {
        translatedText = null;

        if (string.IsNullOrWhiteSpace(originalText) || targetLanguage == SupportedLanguage.Unknown)
            return false;

        var key = GenerateKey(originalText, targetLanguage);

        if (!_cache.TryGetValue(key, out var entry))
            return false;

        if (DateTime.UtcNow - entry.CreatedAt > _ttl)
        {
            _cache.TryRemove(key, out _);
            Interlocked.Add(ref _currentCharacters, -(entry.CharCount));
            return false;
        }

        translatedText = entry.TranslatedText;
        return true;
    }

    public void AddTranslation(string originalText, string translatedText, SupportedLanguage targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(originalText) || string.IsNullOrWhiteSpace(translatedText) ||
            targetLanguage == SupportedLanguage.Unknown)
            return;

        var key = GenerateKey(originalText, targetLanguage);
        var charCount = originalText.Length + translatedText.Length;

        if (_cache.ContainsKey(key))
        {
            _cache[key] = new CacheEntry(translatedText, charCount);
            return;
        }

        while (_currentCharacters + charCount > _maxCharacters && !_cache.IsEmpty)
        {
            var oldest = _cache.MinBy(kvp => kvp.Value.CreatedAt);
            if (_cache.TryRemove(oldest.Key, out var removed))
                Interlocked.Add(ref _currentCharacters, -(removed.CharCount));
        }

        _cache[key] = new CacheEntry(translatedText, charCount);
        Interlocked.Add(ref _currentCharacters, charCount);
    }

    public void SetEngineTag(string engineTag)
    {
        if (_currentEngineTag == engineTag) return;

        _currentEngineTag = engineTag;
        ClearCache();
    }

    public void Configure(int ttlMinutes, int maxCharacters)
    {
        _ttl = TimeSpan.FromMinutes(Math.Max(1, ttlMinutes));
        _maxCharacters = Math.Max(100, maxCharacters);
    }

    public void ClearCache()
    {
        _cache.Clear();
        _currentCharacters = 0;
    }

    private static string GenerateKey(string originalText, SupportedLanguage targetLanguage)
    {
        return $"{targetLanguage}_{originalText.Trim().ToLowerInvariant()}";
    }

    private record CacheEntry(string TranslatedText, int CharCount)
    {
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}