namespace GhostLang.Core.Settings.Audio;

public class VadOptions
{
    public double SilenceThresholdDb { get; set; } = -40.0;

    public int MinSilenceDurationMs { get; set; } = 500;
}