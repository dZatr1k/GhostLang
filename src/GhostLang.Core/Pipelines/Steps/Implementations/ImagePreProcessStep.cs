using GhostLang.Core.Settings.ImagePreProcessing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class ImagePreProcessStep(ImagePreProcessOptions options) : IOptionalPipelineStep
{
    public string StepName => "Image Preprocessing";

    public bool IsEnabled { get; set; }

    public async Task ExecuteAsync(TranslationContext context)
    {
        if (context.IsAborted || !IsEnabled || context.OriginalImage is null)
        {
            context.ProcessedImage = context.OriginalImage;
            return;
        }

        using var msIn = new MemoryStream(context.OriginalImage);
        using var image = await Image.LoadAsync(msIn);

        var isModified = false;

        if (options.Upscale is { IsEnabled: true, Value: > 0 })
        {
            context.ScaleFactor = options.Upscale.Value;
            var newWidth = (int)(image.Width * options.Upscale.Value);
            var newHeight = (int)(image.Height * options.Upscale.Value);

            image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Bicubic));
            isModified = true;
        }

        if (options.GaussianBlur is { IsEnabled: true, Value: > 0 })
        {
            image.Mutate(x => x.GaussianBlur(options.GaussianBlur.Value));
            isModified = true;
        }

        if (options.Grayscale.IsEnabled)
        {
            image.Mutate(x => x.Grayscale());
            isModified = true;
        }

        if (options.AutoLevel.IsEnabled)
        {
            image.Mutate(x => x.HistogramEqualization());
            isModified = true;
        }

        if (options.Brightness is { IsEnabled: true } && Math.Abs(options.Brightness.Value - 1f) > 1e-6f)
        {
            image.Mutate(x => x.Brightness(options.Brightness.Value));
            isModified = true;
        }

        if (options.Contrast is { IsEnabled: true } && Math.Abs(options.Contrast.Value - 1f) > 1e-6f)
        {
            image.Mutate(x => x.Contrast(options.Contrast.Value));
            isModified = true;
        }

        if (options.Sharpen is { IsEnabled: true, Value: > 0 })
        {
            image.Mutate(x => x.GaussianSharpen(options.Sharpen.Value));
            isModified = true;
        }

        if (options.Binarize is { IsEnabled: true, Value: >= 0 })
        {
            var threshold = Math.Clamp(options.Binarize.Value, 0f, 1f);
            image.Mutate(x => x.BinaryThreshold(threshold));
            isModified = true;
        }

        if (options.Invert.IsEnabled)
        {
            image.Mutate(x => x.Invert());
            isModified = true;
        }

        if (isModified)
        {
            using var msOut = new MemoryStream();
            await image.SaveAsPngAsync(msOut);
            context.ProcessedImage = msOut.ToArray();
        }
        else
        {
            context.ProcessedImage = context.OriginalImage;
        }
    }
}