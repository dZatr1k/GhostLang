using GhostLang.Core.Pipelines;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Pipelines.Utilities;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace GhostLang.Core.Settings.Ocr;

public class WindowsOcrEngine : IOcrEngine
{
    public Task<bool> IsLanguageSupportedAsync(SupportedLanguage language)
    {
        var tag = language.ToWindowsLanguageTag();
        var winLang = new Windows.Globalization.Language(tag);
        return Task.FromResult(OcrEngine.IsLanguageSupported(winLang));
    }

    public async Task<List<TextFragment>> RecognizeTextAsync(TranslationContext context,
        List<SupportedLanguage> sourceLanguages)
    {
        var fragments = new List<TextFragment>();

        if (sourceLanguages == null || sourceLanguages.Count == 0)
            return fragments;

        var imageData = context.ProcessedImage ?? context.OriginalImage;
        if (imageData == null || imageData.Length == 0)
            return fragments;

        using var memoryStream = new MemoryStream(imageData);
        using var randomAccessStream = memoryStream.AsRandomAccessStream();

        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        foreach (var lang in sourceLanguages)
        {
            var tag = lang.ToWindowsLanguageTag();
            var winLang = new Windows.Globalization.Language(tag);
            var ocrEngine = OcrEngine.TryCreateFromLanguage(winLang);

            if (ocrEngine == null)
                continue;

            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

            foreach (var line in ocrResult.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.Text))
                    continue;

                var lineBounds = ComputeLineBounds(line);

                var overlap = fragments.FindIndex(f => HasSignificantOverlap(f.Bounds, lineBounds, context.ScaleFactor));

                if (overlap >= 0)
                {
                    if (line.Text.Length > fragments[overlap].OriginalText.Length)
                    {
                        fragments[overlap] = CreateFragment(line.Text, lineBounds, context.ScaleFactor);
                    }
                }
                else
                {
                    fragments.Add(CreateFragment(line.Text, lineBounds, context.ScaleFactor));
                }
            }
        }

        return fragments;
    }

    private static (double X, double Y, double Width, double Height) ComputeLineBounds(OcrLine line)
    {
        var first = line.Words[0].BoundingRect;
        var x = first.X;
        var y = first.Y;
        var maxBottom = first.Y + first.Height;
        var maxRight = first.X + first.Width;

        foreach (var word in line.Words)
        {
            var wb = word.BoundingRect;
            if (wb.X < x) x = wb.X;
            if (wb.Y < y) y = wb.Y;
            if (wb.X + wb.Width > maxRight) maxRight = wb.X + wb.Width;
            if (wb.Y + wb.Height > maxBottom) maxBottom = wb.Y + wb.Height;
        }

        return (x, y, maxRight - x, maxBottom - y);
    }

    private static TextFragment CreateFragment(string text,
        (double X, double Y, double Width, double Height) bounds, double scaleFactor)
    {
        return new TextFragment
        {
            OriginalText = text,
            TranslatedText = string.Empty,
            Bounds = new BoundingBox
            {
                X = (int)(bounds.X / scaleFactor),
                Y = (int)(bounds.Y / scaleFactor),
                Width = (int)(bounds.Width / scaleFactor),
                Height = (int)(bounds.Height / scaleFactor)
            }
        };
    }

    private static bool HasSignificantOverlap(BoundingBox existing,
        (double X, double Y, double Width, double Height) newBounds, double scaleFactor)
    {
        var ex = existing.X * scaleFactor;
        var ey = existing.Y * scaleFactor;
        var ew = existing.Width * scaleFactor;
        var eh = existing.Height * scaleFactor;

        var overlapX = Math.Max(ex, newBounds.X);
        var overlapY = Math.Max(ey, newBounds.Y);
        var overlapRight = Math.Min(ex + ew, newBounds.X + newBounds.Width);
        var overlapBottom = Math.Min(ey + eh, newBounds.Y + newBounds.Height);

        if (overlapRight <= overlapX || overlapBottom <= overlapY)
            return false;

        var overlapArea = (overlapRight - overlapX) * (overlapBottom - overlapY);
        var smallerArea = Math.Min(ew * eh, newBounds.Width * newBounds.Height);

        return smallerArea > 0 && overlapArea / smallerArea > 0.5;
    }
}