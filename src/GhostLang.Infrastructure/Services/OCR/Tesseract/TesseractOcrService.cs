using System.Drawing;
using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using WpfRect = System.Drawing.Rectangle;

namespace GhostLang.Infrastructure.Services.OCR.Tesseract;

public class TesseractOcrService : IOcrService
{
    private readonly string _tessDataPath;

    public TesseractOcrService()
    {
        _tessDataPath = _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
    }

    public async Task<List<OcrBlock>> RecognizeTextAsync(Bitmap screenshot,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var result = new List<OcrBlock>();

            using var processedImage = ImageProcessingService.PrepareForOcr(screenshot);

            try
            {
                var tessPath = Path.GetFullPath(_tessDataPath);
                var languageToUse = "rus+eng";

                using var engine = new TesseractEngine(tessPath, languageToUse, EngineMode.Default);
                using var pix = ConvertBitmapToPix(screenshot);
                using var page = engine.Process(pix);


                using var iter = page.GetIterator();

                iter.Begin();

                do
                {
                    var level = PageIteratorLevel.TextLine;

                    var text = iter.GetText(level);

                    if (string.IsNullOrWhiteSpace(text)) continue;

                    if (iter.TryGetBoundingBox(level, out Rect bounds))
                    {
                        var block = new OcrBlock
                        {
                            Text = text.Trim(),


                            Bounds = new WpfRect(
                                bounds.X1 / 1,
                                bounds.Y1 / 1,
                                bounds.Width / 1,
                                bounds.Height / 1),
                            Confidence = iter.GetConfidence(level)
                        };
                        result.Add(block);
                    }
                } while (iter.Next(PageIteratorLevel.TextLine));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OCR Error: {ex.Message}");
            }

            return result;
        }, cancellationToken);
    }

    private string GetAvailableLanguagesString()
    {
        var foundLangs = new List<string>();

        if (File.Exists(Path.Combine(_tessDataPath, "eng.traineddata")))
            foundLangs.Add("eng");

        if (File.Exists(Path.Combine(_tessDataPath, "rus.traineddata")))
            foundLangs.Add("rus");

        if (foundLangs.Count == 0) return string.Empty;

        return string.Join("+", foundLangs);
    }

    private Pix ConvertBitmapToPix(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);
        memoryStream.Position = 0;
        return Pix.LoadFromMemory(memoryStream.ToArray());
    }
}