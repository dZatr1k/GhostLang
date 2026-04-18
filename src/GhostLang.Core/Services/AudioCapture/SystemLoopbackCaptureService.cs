using System.Diagnostics;
using NAudio.Wave;

namespace GhostLang.Core.Services.AudioCapture;

public class SystemLoopbackCaptureService : IAudioCaptureService
{
    public int SampleRate => 16000;

    public int ChannelCount => 1;

    public bool IsCapturing => _capture is not null;

    public event EventHandler<AudioChunkCapturedEventArgs>? ChunkCaptured;

    private WasapiLoopbackCapture? _capture;
    private MediaFoundationResampler? _resampler;
    private BufferedWaveProvider? _buffer;
    private readonly WaveFormat _targetFormat = new(16000, 16, 1);
    private readonly Stopwatch _stopwatch = new();

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_capture is not null)
            return Task.CompletedTask;

        _capture = new WasapiLoopbackCapture();

        _buffer = new BufferedWaveProvider(_capture.WaveFormat)
        {
            ReadFully = false,
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };

        _resampler = new MediaFoundationResampler(_buffer, _targetFormat)
        {
            ResamplerQuality = 60
        };

        _capture.DataAvailable += OnDataAvailable;

        _stopwatch.Restart();
        _capture.StartRecording();

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_capture is null)
            return Task.CompletedTask;

        _capture.StopRecording();
        _capture.DataAvailable -= OnDataAvailable;
        _capture.Dispose();
        _capture = null;

        _resampler?.Dispose();
        _resampler = null;
        _buffer = null;

        return Task.CompletedTask;
    }

    public void Dispose() => StopAsync().GetAwaiter().GetResult();

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_buffer is null || _resampler is null)
            return;

        _buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);

        var resampled = new byte[_targetFormat.AverageBytesPerSecond];
        int bytesRead = _resampler.Read(resampled, 0, resampled.Length);

        if (bytesRead <= 0)
            return;

        var chunk = new byte[bytesRead];
        Array.Copy(resampled, chunk, bytesRead);

        ChunkCaptured?.Invoke(this, new AudioChunkCapturedEventArgs
        {
            PcmData = chunk,
            TimestampMs = _stopwatch.ElapsedMilliseconds,
            LevelDb = AudioMath.ComputeLevelDb(chunk)
        });
    }
}