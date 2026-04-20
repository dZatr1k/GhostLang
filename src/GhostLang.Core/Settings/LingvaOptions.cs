using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Settings;

public class LingvaOptions : TranslationEngineOptions
{
    public string InstanceUrl { get; set; } = "https://lingva.ml";
}
