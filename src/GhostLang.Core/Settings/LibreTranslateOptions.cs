using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Settings;

public class LibreTranslateOptions : TranslationEngineOptions
{
    public string InstanceUrl { get; set; } = "http://localhost:5000";

    public string ApiKey { get; set; } = string.Empty;
}
