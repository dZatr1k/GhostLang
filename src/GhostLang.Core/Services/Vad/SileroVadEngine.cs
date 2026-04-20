using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace GhostLang.Core.Services.Vad;

public class SileroVadEngine : ISileroVadEngine, IDisposable
{
    private readonly ISileroVadModelManager _modelManager;
    private InferenceSession? _session;

    private const int StateDim1 = 2;
    private const int StateDim2 = 1;
    private const int StateDim3 = 128;
    private const int StateSize = StateDim1 * StateDim2 * StateDim3;

    public SileroVadEngine(ISileroVadModelManager modelManager)
    {
        _modelManager = modelManager;
    }

    public bool IsReady => _session is not null;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        if (_session is not null) return Task.CompletedTask;
        if (!_modelManager.IsModelDownloaded)
            throw new InvalidOperationException(
                $"Silero VAD model not found at {_modelManager.ModelFilePath}. Download it from Settings first.");

        var options = new SessionOptions
        {
            InterOpNumThreads = 1,
            IntraOpNumThreads = 1,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };
        _session = new InferenceSession(_modelManager.ModelFilePath, options);
        return Task.CompletedTask;
    }

    public float[] ComputeFrameProbabilities(byte[] audioInt16Pcm, int sampleRate)
    {
        if (_session is null)
            throw new InvalidOperationException("SileroVadEngine not initialized. Call InitializeAsync first.");
        if (sampleRate != 16000 && sampleRate != 8000)
            throw new ArgumentException("Silero v5 supports only 16kHz or 8kHz audio.", nameof(sampleRate));
        if (audioInt16Pcm.Length == 0)
            return Array.Empty<float>();

        var frameSize = sampleRate == 16000 ? 512 : 256;
        var sampleCount = audioInt16Pcm.Length / 2;
        var frameCount = sampleCount / frameSize;
        if (frameCount == 0) return Array.Empty<float>();

        var samples = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {

            var lo = audioInt16Pcm[i * 2];
            var hi = (sbyte)audioInt16Pcm[i * 2 + 1];
            var s = (short)((hi << 8) | lo);
            samples[i] = s / 32768f;
        }

        var state = new float[StateSize];
        var srTensor = new DenseTensor<long>(new[] { (long)sampleRate }, new[] { 1 });
        var probabilities = new float[frameCount];

        var inputBuffer = new float[frameSize];
        for (var f = 0; f < frameCount; f++)
        {
            Array.Copy(samples, f * frameSize, inputBuffer, 0, frameSize);

            var inputTensor = new DenseTensor<float>(inputBuffer, new[] { 1, frameSize });
            var stateTensor = new DenseTensor<float>(state, new[] { StateDim1, StateDim2, StateDim3 });

            var inputs = new List<NamedOnnxValue>(3)
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor),
                NamedOnnxValue.CreateFromTensor("state", stateTensor),
                NamedOnnxValue.CreateFromTensor("sr", srTensor)
            };

            using var results = _session.Run(inputs);

            float prob = 0f;
            foreach (var r in results)
            {
                if (r.Name == "output")
                {
                    var tensor = r.AsTensor<float>();
                    prob = tensor.GetValue(0);
                }
                else if (r.Name == "stateN")
                {
                    var tensor = r.AsTensor<float>();
                    for (var i = 0; i < StateSize; i++)
                        state[i] = tensor.GetValue(i);
                }
            }

            probabilities[f] = prob;
        }

        return probabilities;
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }
}
