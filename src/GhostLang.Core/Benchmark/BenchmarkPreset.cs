using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Asr;
using GhostLang.Core.Settings.Audio;

namespace GhostLang.Core.Benchmark;

public record BenchmarkPreset(string Name, string Description, Action<AppConfig> Apply);

public static class BenchmarkPresets
{
    public static IReadOnlyList<BenchmarkPreset> Ablation { get; } =
    [
        new("baseline", "All optimizations on (default config)", _ => { }),

        new("no-gpu", "Whisper forced to CPU runtime", cfg =>
        {
            if (cfg.ActiveAsrEngine is WhisperAsrOptions w)
                w.GpuRuntime = WhisperGpuRuntime.Cpu;
        }),

        new("no-cache", "Translation cache check step disabled",
            cfg =>
            {
                cfg.OptionalStepStates["step.image.cachecheck"] = false;
                cfg.OptionalStepStates["step.audio.cachecheck"] = false;
            }),

        new("no-dedup", "Translation dedup disabled — identical fragments hit engine N times",
            cfg => cfg.TranslationDeduplicationEnabled = false),

        new("no-adaptive-fps", "Fixed 500ms screen tick",
            cfg =>
            {
                cfg.AdaptiveFpsEnabled = false;
                cfg.ScreenFastIntervalMs = 500;
                cfg.ScreenSlowIntervalMs = 500;
            }),

        new("rms-vad", "RMS gate instead of Silero ONNX VAD",
            cfg => cfg.VadOptions.Provider = VadProvider.Rms)
    ];
}
