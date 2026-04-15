using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Services;

public interface IScreenTranslationManager
{
    event Action<TranslationContext>? FrameProcessed;
    event Action<string>? StatusChanged;
    event Action? BeforeCapture;
    event Action? AfterCapture;
    event Action? MajorContentChanged;

    bool RecordingMode { get; set; }

    void Start(CaptureRegion region, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguages);
    void Stop();
    void UpdateRegion(CaptureRegion region);
}