using GhostLang.Core.Settings.Erasure;
using OpenCvSharp;

namespace GhostLang.Core.Services.Erasure;

public class OpenCvErasureEngine(OpenCvErasureOptions options) : ITextErasureEngine
{
    public async Task<byte[]> EraseTextAsync(byte[] imagePatch)
    {
        return await Task.Run(() =>
        {
            if (imagePatch.Length == 0)
                return imagePatch;

            using var srcMat = Cv2.ImDecode(imagePatch, ImreadModes.Color);
            if (srcMat.Empty()) 
                return imagePatch;

            using var grayMat = new Mat();
            Cv2.CvtColor(srcMat, grayMat, ColorConversionCodes.BGR2GRAY);

            using var edges = new Mat();
            Cv2.Canny(grayMat, edges, 50, 150);

            Cv2.FindContours(edges, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            using var filledMask = new Mat(edges.Size(), MatType.CV_8UC1, Scalar.Black);
            Cv2.DrawContours(filledMask, contours, -1, Scalar.White, thickness: -1);

            using var maskMat = new Mat();
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            Cv2.Dilate(filledMask, maskMat, kernel, iterations: options.DilationIterations);

            using var dstMat = new Mat();
            var inpaintMethod = options.UseTeleaAlgorithm ? InpaintTypes.Telea : InpaintTypes.NS;
            Cv2.Inpaint(srcMat, maskMat, dstMat, options.InpaintRadius, inpaintMethod);

            Cv2.ImEncode(".png", dstMat, out var resultBytes);
            return resultBytes;
        });
    }
}