using GhostLang.Core.Settings.Asr;

namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class SpeechRecognitionStep(IAsrEngine asrEngine) : IMandatoryAudioPipelineStep, IDisposable
{
    public async Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted)
            return;

        var fragments = await asrEngine.RecognizeAsync(context, context.SourceLanguage);
        context.AudioFragments.AddRange(fragments);
    }

    public void Dispose() => (asrEngine as IDisposable)?.Dispose();
}
