using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using Rect = System.Windows.Rect;

namespace GhostLang.WPF.Helpers;

public static class ImageEffectsHelper
{
    public static BitmapSource? CreateInpaintedBackground(Bitmap fullScreenshot, Rect cropArea)
    {
        var padding = 15;

        var x = (int)Math.Max(0, cropArea.X - padding);
        var y = (int)Math.Max(0, cropArea.Y - padding);

        var w = (int)Math.Min(fullScreenshot.Width - x, cropArea.Width + padding * 2);
        var h = (int)Math.Min(fullScreenshot.Height - y, cropArea.Height + padding * 2);

        if (w <= 0 || h <= 0) return null;

        using var sourcePatch = fullScreenshot.Clone(new Rectangle(x, y, w, h),
            PixelFormat.Format24bppRgb);

        using var srcMat = sourcePatch.ToMat();

        using var maskMat = new Mat(srcMat.Size(), MatType.CV_8UC1, Scalar.Black);

        var maskX = (int)Math.Max(0, cropArea.X - x);
        var maskY = (int)Math.Max(0, cropArea.Y - y);
        
        var maskW = (int)Math.Min(w - maskX, cropArea.Width);
        var maskH = (int)Math.Min(h - maskY, cropArea.Height);

        Cv2.Rectangle(maskMat, new OpenCvSharp.Rect(maskX, maskY, maskW, maskH), Scalar.White, -1);

        using var resultMat = new Mat();
        
        Cv2.Inpaint(srcMat, maskMat, resultMat, 5, InpaintMethod.Telea);

        var finalRect = new OpenCvSharp.Rect(maskX, maskY, maskW, maskH);
        using var finalMat = new Mat(resultMat, finalRect);

        var finalBitmap = finalMat.ToBitmapSource();

        return finalBitmap;
    }

    private static void AddOpenCVNoise(Mat mat, int amount)
    {
        using var noise = new Mat(mat.Size(), mat.Type());
        Cv2.Randn(noise, Scalar.All(0), Scalar.All(amount));
        Cv2.Add(mat, noise, mat);
    }
}