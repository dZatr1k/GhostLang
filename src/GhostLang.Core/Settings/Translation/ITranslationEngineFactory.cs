namespace GhostLang.Core.Settings.Translation;

public interface ITranslationEngineFactory
{
    ITranslationEngine Create(TranslationEngineOptions options);
}
