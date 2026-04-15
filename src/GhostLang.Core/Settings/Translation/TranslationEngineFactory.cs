namespace GhostLang.Core.Settings.Translation;

public class TranslationEngineFactory : ITranslationEngineFactory
{
    public ITranslationEngine Create(TranslationEngineOptions options)
    {
        return options switch
        {
            GTranslateOptions gTranslate => new GTranslateEngine(gTranslate),
            MyMemoryOptions myMemory => new MyMemoryEngine(myMemory),
            LingvaOptions lingva => new LingvaEngine(lingva),
            LibreTranslateOptions libre => new LibreTranslateEngine(libre),
            _ => throw new NotSupportedException($"Translation engine '{options.GetType().Name}' is not supported.")
        };
    }
}
