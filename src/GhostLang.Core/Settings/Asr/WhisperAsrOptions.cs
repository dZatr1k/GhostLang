namespace GhostLang.Core.Settings.Asr;

public enum WhisperGpuRuntime
{

    Auto = 0,

    Vulkan = 1,

    Cpu = 2
}

public class WhisperAsrOptions : AsrEngineOptions
{
    public string ModelName { get; set; } = "ggml-base";

    public string ModelsPath { get; set; } = "Models/Whisper";

    public WhisperGpuRuntime GpuRuntime { get; set; } = WhisperGpuRuntime.Auto;
}
