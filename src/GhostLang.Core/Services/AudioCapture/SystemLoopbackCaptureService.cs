using System.Diagnostics;
using NAudio.Wave;

namespace GhostLang.Core.Services.AudioCapture;

public class SystemLoopbackCaptureService : IAudioCaptureService
{
    public int SampleRate => 16000;

    public int ChannelCount => 1;

    public bool IsCapturing => _capture is not null;

    public event EventHandler<AudioChunkCapturedEventArgs>? ChunkCaptured;

    public event EventHandler<int>? SamplesDropped;

    private readonly int _resamplerQuality;
    private WasapiLoopbackCapture? _capture;
    private MediaFoundationResampler? _resampler;
    private BufferedWaveProvider? _buffer;
    private readonly WaveFormat _targetFormat = new(16000, 16, 1);
    private readonly Stopwatch _stopwatch = new();

    public SystemLoopbackCaptureService(int resamplerQuality = 60)
    {
        _resamplerQuality = Math.Clamp(resamplerQuality, 1, 60);
    }

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
            ResamplerQuality = _resamplerQuality
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

        var freeBytes = _buffer.BufferLength - _buffer.BufferedBytes;
        if (freeBytes < e.BytesRecorded)
        {
            var droppedBytes = e.BytesRecorded - freeBytes;
            SamplesDropped?.Invoke(this, droppedBytes);
        }

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
