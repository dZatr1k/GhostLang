using System.Text.Json;
using GhostLang.Core.Settings;

namespace GhostLang.Core.Services;

public class JsonConfigurationService : IConfigurationService
{
    private const string FilePath = "appsettings.user.json";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly object _cacheLock = new();
    private AppConfig? _cached;

    public event Action<AppConfig>? ConfigChanged;

    public AppConfig Load()
    {
        if (_cached is not null) return _cached;

        lock (_cacheLock)
        {
            if (_cached is not null) return _cached;
            _cached = ReadAndMigrate();
            return _cached;
        }
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, _jsonOptions);

        lock (_cacheLock)
        {

            File.WriteAllText(FilePath, json);
            _cached = config;
        }

        ConfigChanged?.Invoke(config);
    }

    private AppConfig ReadAndMigrate()
    {
        if (!File.Exists(FilePath))
            return new AppConfig();

        var json = File.ReadAllText(FilePath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();

        if (config.HotKeys is not { Count: > 0 })
        {
            config.HotKeys = new AppConfig().HotKeys;
        }
        else
        {
            var defaults = AppConfig.GetDefaultHotKeys();
            foreach (var def in defaults)
            {
                if (!config.HotKeys.Any(h => h.ActionId == def.ActionId))
                    config.HotKeys.Add(def);
            }
        }

        return config;
    }
}
