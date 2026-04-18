using System.Text;
using NAudio.Wave;

namespace GhostLang.Core.Services.AudioCapture;

public static class PcmWavWriter
{
    public static void WritePcm16Mono(string filePath, byte[] pcm, int sampleRate)
    {
        using var writer = new WaveFileWriter(filePath, new WaveFormat(sampleRate, 16, 1));
        writer.Write(pcm, 0, pcm.Length);
    }

    public static byte[] BuildWavBytes(byte[] pcm, int sampleRate)
    {
        const int bitsPerSample = 16;
        const int channels = 1;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int dataSize = pcm.Length;
        int fileSize = 36 + dataSize;

        using var ms = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(ms);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(fileSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write((short)bitsPerSample);

        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        writer.Write(pcm);

        return ms.ToArray();
    }
}