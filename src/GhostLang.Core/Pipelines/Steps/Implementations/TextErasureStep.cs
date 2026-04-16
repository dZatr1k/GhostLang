using GhostLang.Core.Services;
using GhostLang.Core.Services.Erasure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class TextErasureStep(ITextErasureEngine erasureEngine) : IOptionalPipelineStep
{
    public bool IsEnabled { get; set; } = true;

    public string StepName => "Text Erasure";

    public async Task ExecuteAsync(TranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || context.OriginalImage == null || context.TextFragments.Count == 0)
            return;

        context.IsSmartErasureEnabled = IsEnabled && erasureEngine is OpenCvErasureEngine;

        using var msIn = new MemoryStream(context.OriginalImage);
        using var originalImage = await Image.LoadAsync(msIn);

        foreach (var fragment in context.TextFragments)
        {
            var padding = (int)(fragment.Bounds.Height * 0.15);
            var cropRectangle = new Rectangle(
                fragment.Bounds.X - padding,
                fragment.Bounds.Y - padding,
                fragment.Bounds.Width + padding * 2,
                fragment.Bounds.Height + padding * 2
            );

            cropRectangle.Intersect(originalImage.Bounds);

            if (cropRectangle.Width <= 0 || cropRectangle.Height <= 0)
                continue;

            fragment.Bounds.X = cropRectangle.X;
            fragment.Bounds.Y = cropRectangle.Y;
            fragment.Bounds.Width = cropRectangle.Width;
            fragment.Bounds.Height = cropRectangle.Height;

            using var patch = originalImage.Clone(x => x.Crop(cropRectangle));
            using var patchStream = new MemoryStream();
            await patch.SaveAsPngAsync(patchStream);

            var rawPatchBytes = patchStream.ToArray();

            fragment.CleanedPatch = IsEnabled
                ? await erasureEngine.EraseTextAsync(rawPatchBytes)
                : rawPatchBytes;
        }
    }
}