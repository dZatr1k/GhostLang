using NAudio.Wave;

namespace GhostLang.Core.Services.AudioCapture;

public static class PcmWavWriter
{
    public static void WritePcm16Mono(string filePath, byte[] pcm, int sampleRate)
    {
        using var writer = new WaveFileWriter(filePath, new WaveFormat(sampleRate, 16, 1));
        writer.Write(pcm, 0, pcm.Length);
    }
}