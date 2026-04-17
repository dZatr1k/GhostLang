namespace GhostLang.Core.Settings.Asr;

public class AsrEngineFactory : IAsrEngineFactory
{
    public IAsrEngine Create(AsrEngineOptions options)
    {
        throw new NotSupportedException(
            $"ASR engine '{options.GetType().Name}' is not yet implemented. " +
            "Concrete engines are added in Phase 3 (Whisper) and Phase 5 (Vosk, Azure).");
    }
}