namespace GhostLang.Core.Services.AudioCapture;

internal static class AudioMath
{
    public static float ComputeLevelDb(byte[] pcm16)
    {
        if (pcm16.Length < 2)
            return -100f;

        int count = pcm16.Length / 2;
        if (count == 0)
            return -100f;

        double sum = 0;
        for (int i = 0; i + 1 < pcm16.Length; i += 2)
        {
            short sample = BitConverter.ToInt16(pcm16, i);
            double normalized = sample / 32768.0;
            sum += normalized * normalized;
        }

        double rms = Math.Sqrt(sum / count);
        if (rms <= 0)
            return -100f;

        return (float)(20 * Math.Log10(rms));
    }
}
