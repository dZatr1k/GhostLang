using GhostLang.Core.Services.Asr;

namespace GhostLang.Core.Settings.Asr;

public class AsrEngineFactory(IWhisperModelManager whisperModelManager) : IAsrEngineFactory
{
    public IAsrEngine Create(AsrEngineOptions options)
    {
        return options switch
        {
            WhisperAsrOptions whisper => new WhisperAsrEngine(whisper, whisperModelManager),
            _ => throw new NotSupportedException(
                $"ASR engine '{options.GetType().Name}' is not yet implemented. " +
                "Vosk and Azure engines are added in Phase 5.")
        };
    }
}