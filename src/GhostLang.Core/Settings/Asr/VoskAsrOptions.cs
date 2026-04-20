namespace GhostLang.Core.Settings.Asr;

public class VoskAsrOptions : AsrEngineOptions
{

    public string ModelsRootPath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Vosk");

    public string ModelPath { get; set; } = string.Empty;
}
