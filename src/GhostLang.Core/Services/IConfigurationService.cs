using GhostLang.Core.Settings;

namespace GhostLang.Core.Services;

public interface IConfigurationService
{
    AppConfig Load();
    void Save(AppConfig config);
}