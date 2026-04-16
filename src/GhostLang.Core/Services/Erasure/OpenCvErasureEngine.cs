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

            var blockSize = options.AdaptiveBlockSize;
            if (blockSize % 2 == 0) blockSize++;
            if (blockSize < 3) blockSize = 3;

            using var binaryMask = new Mat();
            Cv2.AdaptiveThreshold(grayMat, binaryMask, 255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.BinaryInv, blockSize, options.AdaptiveConstant);

            using var closeKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            using var closedMask = new Mat();
            Cv2.MorphologyEx(binaryMask, closedMask, MorphTypes.Close, closeKernel);

            using var maskMat = new Mat();
            using var dilateKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            Cv2.Dilate(closedMask, maskMat, dilateKernel, iterations: options.DilationIterations);

            using var dstMat = new Mat();
            var inpaintMethod = options.UseTeleaAlgorithm ? InpaintTypes.Telea : InpaintTypes.NS;
            Cv2.Inpaint(srcMat, maskMat, dstMat, options.InpaintRadius, inpaintMethod);

            Cv2.ImEncode(".png", dstMat, out var resultBytes);
            return resultBytes;
        });
    }
}