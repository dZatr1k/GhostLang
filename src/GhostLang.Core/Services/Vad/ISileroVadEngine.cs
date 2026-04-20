namespace GhostLang.Core.Services.Vad;

public interface ISileroVadEngine
{

    bool IsReady { get; }

    Task InitializeAsync(CancellationToken ct = default);

    float[] ComputeFrameProbabilities(byte[] audioInt16Pcm, int sampleRate);
}
