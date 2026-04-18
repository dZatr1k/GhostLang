namespace GhostLang.Core.Settings.Asr;

public class WhisperAsrOptions : AsrEngineOptions
{
    public string ModelName { get; set; } = "ggml-base";

    public string ModelsPath { get; set; } = "Models/Whisper";
}