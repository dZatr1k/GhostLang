using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Settings.Asr;

public interface IAsrEngine
{

    IReadOnlySet<SupportedLanguage> SupportedLanguages { get; }

    bool SupportsStreaming { get; }

    Task<bool> IsLanguageSupportedAsync(SupportedLanguage language);

    Task<List<AudioFragment>> RecognizeAsync(AudioTranslationContext context, List<SupportedLanguage> languages);
}
