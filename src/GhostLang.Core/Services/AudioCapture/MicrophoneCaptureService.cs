using System.Diagnostics;
using NAudio.Wave;

namespace GhostLang.Core.Services.AudioCapture;

public class MicrophoneCaptureService : IAudioCaptureService
{
    public int SampleRate => 16000;

    public int ChannelCount => 1;

    public bool IsCapturing => _waveIn is not null;

    public event EventHandler<AudioChunkCapturedEventArgs>? ChunkCaptured;

#pragma warning disable CS0067
    public event EventHandler<int>? SamplesDropped;
#pragma warning restore CS0067

    private WaveInEvent? _waveIn;
    private readonly Stopwatch _stopwatch = new();

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_waveIn is not null)
            return Task.CompletedTask;

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, 16, ChannelCount),
            BufferMilliseconds = 100
        };
        _waveIn.DataAvailable += OnDataAvailable;

        _stopwatch.Restart();
        _waveIn.StartRecording();

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_waveIn is null)
            return Task.CompletedTask;

        _waveIn.StopRecording();
        _waveIn.DataAvailable -= OnDataAvailable;
        _waveIn.Dispose();
        _waveIn = null;

        return Task.CompletedTask;
    }

    public void Dispose() => StopAsync().GetAwaiter().GetResult();

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var bytes = new byte[e.BytesRecorded];
        Array.Copy(e.Buffer, bytes, e.BytesRecorded);

        ChunkCaptured?.Invoke(this, new AudioChunkCapturedEventArgs
        {
            PcmData = bytes,
            TimestampMs = _stopwatch.ElapsedMilliseconds,
            LevelDb = AudioMath.ComputeLevelDb(bytes)
        });
    }
}
