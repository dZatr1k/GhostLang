using System.Numerics;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Settings;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class TextRenderingStep(TextRenderingOptions options) : IMandatoryPipelineStep
{
    public string StepName => "Text Rendering";

    public async Task ExecuteAsync(TranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || context.OriginalImage == null || context.TextFragments.Count == 0)
            return;

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

        if (!SystemFonts.TryGet(options.SelectedFontFamily, out var fontFamily))
        {
            fontFamily = SystemFonts.Families.FirstOrDefault();
        }

        foreach (var fragment in context.TextFragments)
        {
            if (string.IsNullOrWhiteSpace(fragment.TranslatedText) || fragment.CleanedPatch == null)
                continue;

            if (options.UseOriginalColor)
            {

                var diffColor = await ExtractTextColorByDiffAsync(fragment.OriginalPatch, fragment.CleanedPatch);
                if (diffColor is not null)
                {
                    fragment.TextColorHex = diffColor;
                }
                else
                {
                    var cropRect = new Rectangle(
                        fragment.Bounds.X, fragment.Bounds.Y,
                        fragment.Bounds.Width, fragment.Bounds.Height);

                    cropRect.Intersect(originalImage.Bounds);
                    fragment.TextColorHex = ExtractDominantTextColor(originalImage, cropRect);
                }
            }
            else
            {
                fragment.TextColorHex = options.DefaultColorHex;
            }

            using var patchMs = new MemoryStream(fragment.CleanedPatch);
            using var patchImage = await Image.LoadAsync<Rgba32>(patchMs);

            var baseFont = fontFamily.CreateFont(100f, FontStyle.Bold);
            var textOptions = new RichTextOptions(baseFont)
            {
                Origin = new PointF(0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            var textGlyphs = TextBuilder.GenerateGlyphs(fragment.TranslatedText, textOptions);

            var actualBounds = textGlyphs.Bounds;

            if (actualBounds is { Width: > 0, Height: > 0 })
            {

                float targetHeight = fragment.OriginalTextBounds?.Height > 0
                    ? fragment.OriginalTextBounds.Height
                    : patchImage.Height;
                float targetWidth = fragment.OriginalTextBounds?.Width > 0
                    ? fragment.OriginalTextBounds.Width
                    : patchImage.Width;

                var scaleY = targetHeight / actualBounds.Height;
                var scaleX = scaleY;

                var renderedTextWidth = actualBounds.Width * scaleX;
                var allowExpand = !context.IsSmartErasureEnabled;

                if (renderedTextWidth > targetWidth)
                {
                    if (options.RenderingMode == TextRenderingMode.Compress)
                    {
                        var ratio = renderedTextWidth / targetWidth;
                        if (allowExpand && ratio > 1.5f)
                        {
                            scaleX = targetWidth / actualBounds.Width * 1.5f;
                            targetWidth = actualBounds.Width * scaleX;
                        }
                        else
                        {
                            scaleX = targetWidth / actualBounds.Width;
                        }
                    }
                    else if (allowExpand)
                    {
                        targetWidth = renderedTextWidth;
                    }
                }

                var matrix = Matrix3x2.CreateTranslation(-actualBounds.X, -actualBounds.Y) *
                             Matrix3x2.CreateScale(scaleX, scaleY);

                if (fragment.OriginalTextBounds is { } origBounds)
                {
                    var offsetX = origBounds.X - fragment.Bounds.X;
                    var offsetY = origBounds.Y - fragment.Bounds.Y;
                    matrix *= Matrix3x2.CreateTranslation(offsetX, offsetY);
                }

                textGlyphs = textGlyphs.Transform(matrix);

                var renderWidth = (int)Math.Ceiling(targetWidth);

                if (renderWidth > patchImage.Width)
                {
                    var bgColor = ExtractBackgroundColor(patchImage);

                    patchImage.Mutate(ctx => ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(renderWidth, patchImage.Height),
                        Mode = ResizeMode.BoxPad,
                        PadColor = bgColor,
                        Position = AnchorPositionMode.TopLeft
                    }));

                    fragment.Bounds.Width = renderWidth;
                }
            }

            patchImage.Mutate(ctx =>
            {
                var textColor = Color.ParseHex(fragment.TextColorHex);
                ctx.Fill(textColor, textGlyphs);
            });

            if (fragment.OriginalTextBounds is { } finalOrig)
            {
                var cropX = Math.Max(0, finalOrig.X - fragment.Bounds.X);
                var cropY = Math.Max(0, finalOrig.Y - fragment.Bounds.Y);
                var cropW = Math.Min(patchImage.Width - cropX, Math.Max(finalOrig.Width, fragment.Bounds.Width - cropX));
                var cropH = Math.Min(patchImage.Height - cropY, finalOrig.Height);

                if (cropW > 0 && cropH > 0 && (cropW < patchImage.Width || cropH < patchImage.Height))
                {
                    patchImage.Mutate(ctx => ctx.Crop(new Rectangle(cropX, cropY, cropW, cropH)));

                    fragment.Bounds.X = finalOrig.X;
                    fragment.Bounds.Y = finalOrig.Y;
                    fragment.Bounds.Width = cropW;
                    fragment.Bounds.Height = cropH;
                }
            }

            using var outMs = new MemoryStream();
            await patchImage.SaveAsPngAsync(outMs);
            fragment.RenderedPatch = outMs.ToArray();
        }

        }
        finally
        {
            decodedLocally?.Dispose();
        }
    }

    private static async Task<string?> ExtractTextColorByDiffAsync(byte[]? originalPatch, byte[]? cleanedPatch)
    {
        if (originalPatch is null || cleanedPatch is null ||
            originalPatch.Length == 0 || cleanedPatch.Length == 0)
            return null;

        using var origMs = new MemoryStream(originalPatch);
        using var cleanedMs = new MemoryStream(cleanedPatch);
        using var origImage = await Image.LoadAsync<Rgba32>(origMs);
        using var cleanedImage = await Image.LoadAsync<Rgba32>(cleanedMs);

        if (origImage.Width != cleanedImage.Width || origImage.Height != cleanedImage.Height)
            return null;

        var candidates = new List<(int distance, byte r, byte g, byte b)>(origImage.Width * origImage.Height);

        for (var y = 0; y < origImage.Height; y++)
        {
            for (var x = 0; x < origImage.Width; x++)
            {
                var origPx = origImage[x, y];
                var cleanedPx = cleanedImage[x, y];
                var distance = Math.Abs(origPx.R - cleanedPx.R)
                             + Math.Abs(origPx.G - cleanedPx.G)
                             + Math.Abs(origPx.B - cleanedPx.B);
                if (distance > 30)
                    candidates.Add((distance, origPx.R, origPx.G, origPx.B));
            }
        }

        var minPixelCount = Math.Max(5, origImage.Width * origImage.Height / 200);
        if (candidates.Count < minPixelCount)
            return null;

        candidates.Sort((a, b) => b.distance.CompareTo(a.distance));

        var bodyCount = Math.Max(minPixelCount, candidates.Count / 4);

        long sumR = 0, sumG = 0, sumB = 0;
        for (var i = 0; i < bodyCount; i++)
        {
            sumR += candidates[i].r;
            sumG += candidates[i].g;
            sumB += candidates[i].b;
        }

        var meanR = (byte)(sumR / bodyCount);
        var meanG = (byte)(sumG / bodyCount);
        var meanB = (byte)(sumB / bodyCount);

        return $"#{meanR:X2}{meanG:X2}{meanB:X2}";
    }

    private static Color ExtractBackgroundColor(Image<Rgba32> image)
    {
        var colorFreq = new Dictionary<Rgba32, int>();
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                colorFreq.TryGetValue(pixel, out var count);
                colorFreq[pixel] = count + 1;
            }
        }

        var dominant = colorFreq.MaxBy(kvp => kvp.Value).Key;
        return new Color(dominant);
    }

    private string ExtractDominantTextColor(Image<Rgba32> image, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return options.DefaultColorHex;

        var colorFreq = new Dictionary<Rgba32, int>();

        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (var x = bounds.Left; x < bounds.Right; x++)
            {
                var pixel = image[x, y];
                if (colorFreq.TryGetValue(pixel, out var value))
                    colorFreq[pixel] = ++value;
                else
                    colorFreq[pixel] = 1;
            }
        }

        var sortedColors = colorFreq.OrderByDescending(kvp => kvp.Value).ToList();
        if (sortedColors.Count == 0) return options.DefaultColorHex;

        var bgColor = sortedColors.First().Key;

        foreach (var kvp in sortedColors.Skip(1))
        {
            var color = kvp.Key;
            var distance = Math.Sqrt(
                Math.Pow(color.R - bgColor.R, 2) +
                Math.Pow(color.G - bgColor.G, 2) +
                Math.Pow(color.B - bgColor.B, 2)
            );

            if (distance > 60)
            {
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }

        return options.DefaultColorHex;
    }
}
