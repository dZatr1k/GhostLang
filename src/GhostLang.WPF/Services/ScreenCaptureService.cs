using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GhostLang.Core.Services;

namespace GhostLang.WPF.Services;

public class ScreenCaptureService : IScreenCaptureService
{
    public byte[] CaptureRegion(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return [];

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
