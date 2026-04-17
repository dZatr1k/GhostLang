namespace GhostLang.Core.Services.AudioCapture;

public interface IAudioCaptureService : IDisposable
{
    int SampleRate { get; }

    int ChannelCount { get; }

    bool IsCapturing { get; }

    event EventHandler<AudioChunkCapturedEventArgs>? ChunkCaptured;

    Task StartAsync(CancellationToken ct = default);

    Task StopAsync();
}