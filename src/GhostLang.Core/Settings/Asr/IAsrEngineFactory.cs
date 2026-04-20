namespace GhostLang.Core.Settings.Asr;

public interface IAsrEngineFactory
{
    IAsrEngine Create(AsrEngineOptions options);
}
