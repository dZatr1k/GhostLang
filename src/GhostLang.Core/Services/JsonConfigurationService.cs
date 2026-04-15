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

    public AppConfig Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppConfig();
        }

        var json = File.ReadAllText(FilePath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();

        // Restore defaults for collections that must not be empty
        if (config.HotKeys is not { Count: > 0 })
        {
            config.HotKeys = new AppConfig().HotKeys;
        }

        return config;
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(FilePath, json);
    }
}