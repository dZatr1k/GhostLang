namespace GhostLang.Core.Settings.Audio;

public class SubtitleOptions
{
    public bool ShowOriginal { get; set; } = true;

    public string Position { get; set; } = "Bottom";

    public int MonitorIndex { get; set; } = -1;

    public int MinDurationMs { get; set; } = 1500;

    public int MaxDurationMs { get; set; } = 8000;

    public int MaxCharsBeforeEarlyHide { get; set; } = 400;
}
