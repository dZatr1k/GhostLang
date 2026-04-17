using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Settings.Asr;

public interface IAsrEngine
{
    Task<bool> IsLanguageSupportedAsync(SupportedLanguage language);

    Task<List<AudioFragment>> RecognizeAsync(AudioTranslationContext context, List<SupportedLanguage> languages);
}