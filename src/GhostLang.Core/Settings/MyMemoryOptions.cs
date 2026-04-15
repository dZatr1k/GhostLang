using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Settings;

public class MyMemoryOptions : TranslationEngineOptions
{
    public string Email { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}