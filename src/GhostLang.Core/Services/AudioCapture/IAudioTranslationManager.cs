using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Services.AudioCapture;

public interface IAudioTranslationManager
{
    bool IsActive { get; }

    event EventHandler<AudioTranslationSessionEventArgs>? FragmentsReady;

    event EventHandler<string>? StatusChanged;

    event EventHandler<float>? LevelChanged;

    Task StartAsync(AudioCaptureSource source, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguages);

    Task StopAsync();
}