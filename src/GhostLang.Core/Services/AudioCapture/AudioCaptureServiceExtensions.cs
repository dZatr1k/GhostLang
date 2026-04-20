namespace GhostLang.Core.Services.AudioCapture;

public static class AudioCaptureServiceExtensions
{
    public static async Task<byte[]> CaptureForDurationAsync(
        this IAudioCaptureService service,
        TimeSpan duration,
        CancellationToken ct = default)
    {
        var buffer = new List<byte>();

        void Handler(object? _, AudioChunkCapturedEventArgs e)
        {
            lock (buffer)
            {
                buffer.AddRange(e.PcmData);
            }
        }

        service.ChunkCaptured += Handler;
        try
        {
            await service.StartAsync(ct);
            await Task.Delay(duration, ct);
            await service.StopAsync();
        }
        finally
        {
            service.ChunkCaptured -= Handler;
        }

        lock (buffer)
        {
            return buffer.ToArray();
        }
    }
}
