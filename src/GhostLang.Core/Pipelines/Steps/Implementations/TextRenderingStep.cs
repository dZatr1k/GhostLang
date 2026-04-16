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

        using var msIn = new MemoryStream(context.OriginalImage);
        using var originalImage = await Image.LoadAsync<Rgba32>(msIn);

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
                var cropRect = new Rectangle(
                    fragment.Bounds.X, fragment.Bounds.Y,
                    fragment.Bounds.Width, fragment.Bounds.Height);

                cropRect.Intersect(originalImage.Bounds);
                fragment.TextColorHex = ExtractDominantTextColor(originalImage, cropRect);
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
                float targetHeight = patchImage.Height;
                float targetWidth = patchImage.Width;

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

            using var outMs = new MemoryStream();
            await patchImage.SaveAsPngAsync(outMs);
            fragment.RenderedPatch = outMs.ToArray();
        }
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