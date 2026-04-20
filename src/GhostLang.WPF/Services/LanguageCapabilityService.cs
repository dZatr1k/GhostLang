using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Utilities;
using GhostLang.Core.Services;
using GhostLang.Core.Settings.Asr;
using GhostLang.Core.Settings.Ocr;
using GhostLang.Core.Settings.Translation;

namespace GhostLang.WPF.Services;

public class LanguageCapabilityService(
    IConfigurationService configService,
    IOcrEngineFactory ocrFactory,
    IAsrEngineFactory asrFactory,
    ITranslationEngineFactory translationFactory)
{
    public event Action? Changed;

    public IReadOnlySet<SupportedLanguage> GetScreenLanguages()
    {
        var config = configService.Load();
        var ocr = GetOcrSet(config.ActiveOcrEngine);
        var tr = GetTranslationSet(config.ActiveTranslationEngine);
        return Intersect(ocr, tr);
    }

    public IReadOnlySet<SupportedLanguage> GetAudioLanguages()
    {
        var config = configService.Load();
        var asr = GetAsrSet(config.ActiveAsrEngine);
        var tr = GetTranslationSet(config.ActiveTranslationEngine);
        return Intersect(asr, tr);
    }

    public void NotifyChanged() => Changed?.Invoke();

    private IReadOnlySet<SupportedLanguage> GetOcrSet(OcrEngineOptions? options)
    {
        if (options is null) return LanguageCapabilitySets.AllTwenty;

        var engine = ocrFactory.Create(options);
        var set = engine.SupportedLanguages;
        (engine as IDisposable)?.Dispose();
        return set;
    }

    private IReadOnlySet<SupportedLanguage> GetAsrSet(AsrEngineOptions? options)
    {
        if (options is null) return LanguageCapabilitySets.AllTwenty;
        var engine = asrFactory.Create(options);
        var set = engine.SupportedLanguages;
        (engine as IDisposable)?.Dispose();
        return set;
    }

    private IReadOnlySet<SupportedLanguage> GetTranslationSet(TranslationEngineOptions? options)
    {
        if (options is null) return LanguageCapabilitySets.AllTwenty;
        var engine = translationFactory.Create(options);
        var set = engine.SupportedLanguages;
        (engine as IDisposable)?.Dispose();
        return set;
    }

    private static IReadOnlySet<SupportedLanguage> Intersect(
        IReadOnlySet<SupportedLanguage> a, IReadOnlySet<SupportedLanguage> b)
    {
        var result = new HashSet<SupportedLanguage>();
        foreach (var lang in a)
            if (b.Contains(lang)) result.Add(lang);
        return result;
    }
}
