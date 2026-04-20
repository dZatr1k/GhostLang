using System.Security.Cryptography;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class MotionDetectionStep : IOptionalPipelineStep
{
    public bool IsEnabled { get; set; } = true;

    private byte[]? _previousFrameHash;
    private readonly object _hashLock = new();

    public string StepName => "Motion Detection";

    public Task ExecuteAsync(TranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || !IsEnabled || context.OriginalImage == null)
            return Task.CompletedTask;

        var currentHash = MD5.HashData(context.OriginalImage);

        lock (_hashLock)
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

    public void Reset()
    {
        lock (_hashLock)
        {
            _previousFrameHash = null;
        }
    }
}
