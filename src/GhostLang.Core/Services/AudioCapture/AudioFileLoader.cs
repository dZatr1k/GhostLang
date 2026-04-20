using NAudio.Wave;

namespace GhostLang.Core.Services.AudioCapture;

public static class AudioFileLoader
{
    public const int TargetSampleRate = 16000;
    public const int TargetBitsPerSample = 16;
    public const int TargetChannels = 1;

    public static byte[] LoadAsPcm16Mono16kHz(string filePath)
    {
        using var reader = new MediaFoundationReader(filePath);

        if (reader.WaveFormat.SampleRate == TargetSampleRate &&
            reader.WaveFormat.Channels == TargetChannels &&
            reader.WaveFormat.BitsPerSample == TargetBitsPerSample &&
            reader.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            using var ms = new MemoryStream();
            reader.CopyTo(ms);
            return ms.ToArray();
        }

        var targetFormat = new WaveFormat(TargetSampleRate, TargetBitsPerSample, TargetChannels);
        using var resampler = new MediaFoundationResampler(reader, targetFormat)
        {
            ResamplerQuality = 60
        };

        using var resampled = new MemoryStream();
        var buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
        {
            resampled.Write(buffer, 0, bytesRead);
        }

        return resampled.ToArray();
    }
}
