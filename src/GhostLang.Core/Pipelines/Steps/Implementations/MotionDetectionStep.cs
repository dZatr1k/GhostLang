using System.Security.Cryptography;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class MotionDetectionStep : IOptionalPipelineStep
{
    public bool IsEnabled { get; set; } = true;

    private static byte[]? _previousFrameHash;
    private static readonly object HashLock = new();

    public string StepName => "Motion Detection";

    public Task ExecuteAsync(TranslationContext context)
    {
        if (context.IsAborted || !IsEnabled || context.OriginalImage == null)
            return Task.CompletedTask;

        var currentHash = MD5.HashData(context.OriginalImage);

        lock (HashLock)
        {
            if (_previousFrameHash != null && currentHash.AsSpan().SequenceEqual(_previousFrameHash))
            {
                context.IsAborted = true;
                return Task.CompletedTask;
            }

            _previousFrameHash = currentHash;
        }

        return Task.CompletedTask;
    }

    public static void ResetState()
    {
        lock (HashLock)
        {
            _previousFrameHash = null;
        }
    }
}
