using GhostLang.Core.Settings.Erasure;
using OpenCvSharp;

namespace GhostLang.Core.Services.Erasure;

public class OpenCvErasureEngine(OpenCvErasureOptions options) : ITextErasureEngine
{

    private const int SmallTextHeightThreshold = 20;

    private const double DarkBackgroundMeanLuminance = 128.0;

    public async Task<byte[]> EraseTextAsync(byte[] imagePatch)
    {
        return await Task.Run(() =>
        {
            if (imagePatch.Length == 0)
                return imagePatch;

            using var srcMat = Cv2.ImDecode(imagePatch, ImreadModes.Color);
            if (srcMat.Empty())
                return imagePatch;

            Mat workingSrc = srcMat;
            Mat? upscaledSrc = null;
            var wasUpscaled = srcMat.Rows < SmallTextHeightThreshold || srcMat.Cols < SmallTextHeightThreshold;
            if (wasUpscaled)
            {
                upscaledSrc = new Mat();
                Cv2.Resize(srcMat, upscaledSrc,
                    new Size(srcMat.Cols * 2, srcMat.Rows * 2),
                    interpolation: InterpolationFlags.Cubic);
                workingSrc = upscaledSrc;
            }

            try
            {
                using var grayMat = new Mat();
                Cv2.CvtColor(workingSrc, grayMat, ColorConversionCodes.BGR2GRAY);

                var meanLuminance = Cv2.Mean(grayMat).Val0;
                var isDarkBackground = meanLuminance < DarkBackgroundMeanLuminance;

                using var thresholdInput = new Mat();
                if (isDarkBackground)
                    Cv2.BitwiseNot(grayMat, thresholdInput);
                else
                    grayMat.CopyTo(thresholdInput);

                var autoBlockSize = Math.Max(3, workingSrc.Rows / 3);
                var effectiveBlockSize = Math.Max(options.AdaptiveBlockSize, autoBlockSize);
                if (effectiveBlockSize % 2 == 0) effectiveBlockSize++;
                if (effectiveBlockSize < 3) effectiveBlockSize = 3;

                using var binaryMask = new Mat();
                Cv2.AdaptiveThreshold(thresholdInput, binaryMask, 255,
                    AdaptiveThresholdTypes.GaussianC,
                    ThresholdTypes.BinaryInv, effectiveBlockSize, options.AdaptiveConstant);

                using var closeKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                using var closedMask = new Mat();
                Cv2.MorphologyEx(binaryMask, closedMask, MorphTypes.Close, closeKernel);

                using var maskMat = new Mat();
                using var dilateKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                Cv2.Dilate(closedMask, maskMat, dilateKernel, iterations: options.DilationIterations);

                using var dstMat = new Mat();
                var inpaintMethod = options.UseTeleaAlgorithm ? InpaintTypes.Telea : InpaintTypes.NS;
                Cv2.Inpaint(workingSrc, maskMat, dstMat, options.InpaintRadius, inpaintMethod);

                Mat finalResult;
                Mat? downscaled = null;
                if (wasUpscaled)
                {
                    downscaled = new Mat();
                    Cv2.Resize(dstMat, downscaled,
                        new Size(srcMat.Cols, srcMat.Rows),
                        interpolation: InterpolationFlags.Area);
                    finalResult = downscaled;
                }
                else
                {
                    finalResult = dstMat;
                }

                try
                {
                    Cv2.ImEncode(".png", finalResult, out var resultBytes);
                    return resultBytes;
                }
                finally
                {
                    downscaled?.Dispose();
                }
            }
            finally
            {
                upscaledSrc?.Dispose();
            }
        });
    }
}
