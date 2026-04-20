using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Services;

public partial class TranslationCacheService : ITranslationCacheService, IDisposable
{
    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private string _currentEngineTag = string.Empty;
    private TimeSpan _ttl = TimeSpan.FromMinutes(60);
    private int _maxCharacters = 10000;
    private int _currentCharacters;

    private const string CacheFileName = "translation-cache.json";
    private const int SaveDebounceMs = 2000;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _cacheFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, CacheFileName);
    private readonly Timer _saveTimer;
    private readonly object _saveLock = new();
    private bool _disposed;

    public TranslationCacheService()
    {
        _saveTimer = new Timer(_ => FlushNow(), null, Timeout.Infinite, Timeout.Infinite);
        TryLoad();
    }

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
            ScheduleSave();
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
        ScheduleSave();
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
        ScheduleSave();
    }

    private static string GenerateKey(string originalText, SupportedLanguage targetLanguage)
    {

        var normalized = WhitespaceRegex().Replace(originalText.Trim(), " ").ToLowerInvariant();
        return $"{targetLanguage}_{normalized}";
    }

    private void ScheduleSave()
    {
        if (_disposed) return;

        _saveTimer.Change(SaveDebounceMs, Timeout.Infinite);
    }

    private void FlushNow()
    {
        if (_disposed) return;
        lock (_saveLock)
        {
            try
            {

                var snapshot = new CacheFile
                {
                    EngineTag = _currentEngineTag,
                    Entries = _cache.ToArray()
                        .Select(kv => new PersistedEntry
                        {
                            Key = kv.Key,
                            TranslatedText = kv.Value.TranslatedText,
                            CharCount = kv.Value.CharCount,
                            CreatedAtUtc = kv.Value.CreatedAt
                        })
                        .ToList()
                };

                var json = JsonSerializer.Serialize(snapshot, JsonOptions);

                var tempPath = _cacheFilePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _cacheFilePath, overwrite: true);
            }
            catch
            {

            }
        }
    }

    private void TryLoad()
    {
        if (!File.Exists(_cacheFilePath)) return;

        try
        {
            var json = File.ReadAllText(_cacheFilePath);
            var snapshot = JsonSerializer.Deserialize<CacheFile>(json, JsonOptions);
            if (snapshot is null) return;

            _currentEngineTag = snapshot.EngineTag ?? string.Empty;

            var cutoff = DateTime.UtcNow - _ttl;
            long total = 0;
            foreach (var persisted in snapshot.Entries ?? new List<PersistedEntry>())
            {
                if (string.IsNullOrEmpty(persisted.Key) || string.IsNullOrEmpty(persisted.TranslatedText))
                    continue;

                if (persisted.CreatedAtUtc < cutoff) continue;

                var entry = new CacheEntry(persisted.TranslatedText, persisted.CharCount)
                {
                    CreatedAt = persisted.CreatedAtUtc
                };
                _cache[persisted.Key] = entry;
                total += persisted.CharCount;
            }

            _currentCharacters = (int)Math.Min(int.MaxValue, total);
        }
        catch
        {

        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _saveTimer.Dispose();

        FlushNow();
    }

    private class CacheEntry
    {
        public string TranslatedText { get; }
        public int CharCount { get; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public CacheEntry(string translatedText, int charCount)
        {
            TranslatedText = translatedText;
            CharCount = charCount;
        }
    }

    private class PersistedEntry
    {
        public string? Key { get; set; }
        public string? TranslatedText { get; set; }
        public int CharCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    private class CacheFile
    {
        public string? EngineTag { get; set; }
        public List<PersistedEntry>? Entries { get; set; }
    }
}
