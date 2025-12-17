using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GhostLang.Infrastructure.Services;

#pragma warning disable CA1416
public static class ImageProcessingService
{
    public static readonly float ScaleFactor = 3.0f;
    public static Bitmap PrepareForOcr(Bitmap original)
    {
        var scaleFactor = ScaleFactor;
        using var resized = Resize(original, scaleFactor);

        return AdjustContrastAndGrayscale(resized);
    }

    private static Bitmap Resize(Bitmap original, float scaleFactor)
    {
        var newWidth = (int)(original.Width * scaleFactor);
        var newHeight = (int)(original.Height * scaleFactor);

        var resizedImage = new Bitmap(newWidth, newHeight);

        resizedImage.SetResolution(original.HorizontalResolution, original.VerticalResolution);

        using var graphics = Graphics.FromImage(resizedImage);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        graphics.DrawImage(original, 0, 0, newWidth, newHeight);

        return resizedImage;
    }

    private static Bitmap AdjustContrastAndGrayscale(Bitmap original)
    {
        var newBitmap = new Bitmap(original.Width, original.Height);

        using var graphics = Graphics.FromImage(newBitmap);
        var c = 1.5f;
        var t = (1.0f - c) / 2.0f;

        ColorMatrix colorMatrix = new ColorMatrix(
        [
            [c * 0.3f, c * 0.3f, c * 0.3f, 0, 0],
            [c * 0.59f, c * 0.59f, c * 0.59f, 0, 0],
            [c * 0.11f, c * 0.11f, c * 0.11f, 0, 0],
            [0, 0, 0, 1, 0],
            [t, t, t, 0, 1]
        ]);

        var attributes = new ImageAttributes();
        attributes.SetColorMatrix(colorMatrix);

        graphics.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
            0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

        return newBitmap;
    }
}
#pragma warning restore CA1416