namespace GhostLang.Core.Settings.Audio;

public enum VadProvider
{

    Rms = 0,

    Silero = 1
}

public class VadOptions
{
    public VadProvider Provider { get; set; } = VadProvider.Rms;

    public double SilenceThresholdDb { get; set; } = -40.0;

    public float SpeechProbabilityThreshold { get; set; } = 0.5f;

    public int MinSilenceDurationMs { get; set; } = 500;
}
