using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.WPF.Services;

public interface IAudioTranslationManager
{
    bool IsActive { get; }

    event EventHandler<AudioTranslationSessionEventArgs>? FragmentsReady;

    event EventHandler<PipelineStatus>? StatusChanged;

    event EventHandler<float>? LevelChanged;

    event EventHandler<long>? DriftChanged;

    Task StartAsync(AudioCaptureSource source, SupportedLanguage targetLanguage, List<SupportedLanguage> sourceLanguages);

    Task StopAsync();
}
