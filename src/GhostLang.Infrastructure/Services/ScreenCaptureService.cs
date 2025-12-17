using System.Drawing;
using System.Drawing.Imaging;
using GhostLang.Application.Interfaces;

namespace GhostLang.Infrastructure.Services;

public class ScreenCaptureService: IScreenCaptureService
{
    public Bitmap CaptureScreenArea(RectangleF area)
    {
        
        int x = (int)Math.Round(area.X);
        int y = (int)Math.Round(area.Y);
        int width = (int)Math.Round(area.Width);
        int height = (int)Math.Round(area.Height);

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("Размеры области должны быть положительными.");
        }

        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using Graphics g = Graphics.FromImage(bmp);
        g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

        return bmp;
    }
}