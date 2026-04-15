using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.Core.Settings;

public class GTranslateOptions : TranslationEngineOptions
{
    public GTranslateProvider Provider { get; set; } = GTranslateProvider.Google;
}