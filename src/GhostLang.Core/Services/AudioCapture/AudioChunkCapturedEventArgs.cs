namespace GhostLang.Core.Services.AudioCapture;

public class AudioChunkCapturedEventArgs : EventArgs
{
    public byte[] PcmData { get; init; } = Array.Empty<byte>();

    public long TimestampMs { get; init; }

    public float LevelDb { get; init; }
}