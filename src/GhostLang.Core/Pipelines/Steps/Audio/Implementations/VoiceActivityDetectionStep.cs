using GhostLang.Core.Services.AudioCapture;
using GhostLang.Core.Services.Vad;
using GhostLang.Core.Settings.Audio;

namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class VoiceActivityDetectionStep : IOptionalAudioPipelineStep
{
    private const int FrameDurationMs = 32;

    private readonly VadOptions _options;
    private readonly ISileroVadEngine _sileroEngine;
    private readonly ISileroVadModelManager _sileroModelManager;
    private bool _sileroInitializedOnce;

    public VoiceActivityDetectionStep(VadOptions options,
        ISileroVadEngine sileroEngine,
        ISileroVadModelManager sileroModelManager)
    {
        _options = options;
        _sileroEngine = sileroEngine;
        _sileroModelManager = sileroModelManager;
    }

    public bool IsEnabled { get; set; } = true;

    public Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || !IsEnabled || context.OriginalAudio is null || context.OriginalAudio.Length == 0)
            return Task.CompletedTask;

        var mask = BuildSpeechMask(context);
        if (mask.Length == 0)
        {
            context.IsAborted = true;
            return Task.CompletedTask;
        }

        BridgeShortSilence(mask, context.SampleRate);

        if (!HasSpeech(mask))
        {
            context.IsAborted = true;
            return Task.CompletedTask;
        }

        TrimEdges(context, mask);
        return Task.CompletedTask;
    }

    private bool[] BuildSpeechMask(AudioTranslationContext context)
    {
        var audio = context.OriginalAudio!;
        var sampleRate = context.SampleRate;

        if (_options.Provider == VadProvider.Silero && TryEnsureSileroReady())
        {
            try
            {
                var probs = _sileroEngine.ComputeFrameProbabilities(audio, sampleRate);
                var threshold = _options.SpeechProbabilityThreshold;
                var mask = new bool[probs.Length];
                for (var i = 0; i < probs.Length; i++) mask[i] = probs[i] >= threshold;
                return mask;
            }
            catch
            {

            }
        }

        return BuildRmsMask(audio, sampleRate);
    }

    private bool TryEnsureSileroReady()
    {
        if (_sileroEngine.IsReady) return true;
        if (_sileroInitializedOnce) return false;
        _sileroInitializedOnce = true;

        if (!_sileroModelManager.IsModelDownloaded) return false;
        try
        {
            _sileroEngine.InitializeAsync().GetAwaiter().GetResult();
            return _sileroEngine.IsReady;
        }
        catch
        {
            return false;
        }
    }

    private bool[] BuildRmsMask(byte[] audio, int sampleRate)
    {
        var samplesPerFrame = sampleRate * FrameDurationMs / 1000;
        var bytesPerFrame = samplesPerFrame * 2;
        var frameCount = audio.Length / bytesPerFrame;
        var mask = new bool[frameCount];

        for (var f = 0; f < frameCount; f++)
        {
            var frameSegment = new byte[bytesPerFrame];
            Array.Copy(audio, f * bytesPerFrame, frameSegment, 0, bytesPerFrame);
            var db = AudioMath.ComputeLevelDb(frameSegment);
            mask[f] = db >= _options.SilenceThresholdDb;
        }
        return mask;
    }

    private void BridgeShortSilence(bool[] mask, int sampleRate)
    {
        if (_options.MinSilenceDurationMs <= 0) return;

        var frameMs = GetFrameMs(sampleRate);
        var minSilenceFrames = Math.Max(1, _options.MinSilenceDurationMs / frameMs);

        var i = 0;
        while (i < mask.Length)
        {
            if (mask[i]) { i++; continue; }
            var runStart = i;
            while (i < mask.Length && !mask[i]) i++;
            var runLen = i - runStart;

            var isInterior = runStart > 0 && i < mask.Length;
            if (isInterior && runLen < minSilenceFrames)
            {
                for (var k = runStart; k < i; k++) mask[k] = true;
            }
        }
    }

    private static bool HasSpeech(bool[] mask)
    {
        for (var i = 0; i < mask.Length; i++)
            if (mask[i]) return true;
        return false;
    }

    private void TrimEdges(AudioTranslationContext context, bool[] mask)
    {
        var audio = context.OriginalAudio!;
        var sampleRate = context.SampleRate;
        var frameMs = GetFrameMs(sampleRate);
        var bytesPerFrame = sampleRate * frameMs / 1000 * 2;

        var firstSpeech = 0;
        while (firstSpeech < mask.Length && !mask[firstSpeech]) firstSpeech++;
        var lastSpeech = mask.Length - 1;
        while (lastSpeech >= 0 && !mask[lastSpeech]) lastSpeech--;

        if (firstSpeech == 0 && lastSpeech == mask.Length - 1) return;

        var startByte = firstSpeech * bytesPerFrame;
        var endByte = (lastSpeech + 1) * bytesPerFrame;
        if (endByte > audio.Length) endByte = audio.Length;

        var trimmed = new byte[endByte - startByte];
        Array.Copy(audio, startByte, trimmed, 0, trimmed.Length);
        context.OriginalAudio = trimmed;

        if (context.CaptureStartMs.HasValue && firstSpeech > 0)
            context.CaptureStartMs = context.CaptureStartMs.Value + firstSpeech * frameMs;
    }

    private static int GetFrameMs(int sampleRate) => FrameDurationMs;
}
