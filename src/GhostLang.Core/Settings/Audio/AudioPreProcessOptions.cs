using GhostLang.Core.Settings.ImagePreProcessing;

namespace GhostLang.Core.Settings.Audio;

public class AudioPreProcessOptions
{
    public FilterOption Resample16kHz { get; set; } = new() { IsEnabled = true };

    public FilterOption NormalizeLoudness { get; set; } = new() { IsEnabled = false };

    public FilterOption HighPassFilter { get; set; } = new() { IsEnabled = false };

    public FilterOption NoiseSuppression { get; set; } = new() { IsEnabled = false };
}