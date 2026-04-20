using GhostLang.Core.Services;
using GhostLang.Core.Services.Erasure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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

        Image<Rgba32>? decodedLocally = null;
        var originalImage = context.OriginalPixels;
        if (originalImage is null)
        {
            using var msIn = new MemoryStream(context.OriginalImage);
            decodedLocally = await Image.LoadAsync<Rgba32>(msIn);
            originalImage = decodedLocally;
        }

        try
        {

        foreach (var fragment in context.TextFragments)
        {

            fragment.OriginalTextBounds = new Models.BoundingBox
            {
                X = fragment.Bounds.X,
                Y = fragment.Bounds.Y,
                Width = fragment.Bounds.Width,
                Height = fragment.Bounds.Height
            };

            var padTop = Math.Max(4, (int)(fragment.Bounds.Height * 0.25));
            var padBottom = Math.Max(6, (int)(fragment.Bounds.Height * 0.35));
            var padH = Math.Max(3, (int)(fragment.Bounds.Height * 0.15));

            var cropRectangle = new Rectangle(
                fragment.Bounds.X - padH,
                fragment.Bounds.Y - padTop,
                fragment.Bounds.Width + padH * 2,
                fragment.Bounds.Height + padTop + padBottom
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

            fragment.OriginalPatch = rawPatchBytes;

            fragment.CleanedPatch = IsEnabled
                ? await erasureEngine.EraseTextAsync(rawPatchBytes)
                : rawPatchBytes;
        }

        }
        finally
        {

            decodedLocally?.Dispose();
        }
    }
}
