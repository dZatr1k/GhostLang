using System.Globalization;
using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Settings.Ocr;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class OcrStep(IOcrEngine ocrEngine) : IMandatoryPipelineStep, IDisposable
{
    public string StepName => "Text Recognition (OCR)";

    public void Dispose() => (ocrEngine as IDisposable)?.Dispose();

    public async Task ExecuteAsync(TranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || context.ProcessedImage == null)
            return;

        var fragments = await ocrEngine.RecognizeTextAsync(context, context.SourceLanguage);

        context.TextFragments.AddRange(fragments.Where(IsValidFragment));
    }

    private static bool IsValidFragment(TextFragment fragment)
    {
        var text = fragment.OriginalText;

        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
            return false;

        if (fragment.Bounds.Width < 3 || fragment.Bounds.Height < 8)
            return false;

        var letterCount = 0;
        var junkCount = 0;

        foreach (var ch in text)
        {

            var category = CharUnicodeInfo.GetUnicodeCategory(ch);

            var isLetter = category is
                UnicodeCategory.UppercaseLetter or
                UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or
                UnicodeCategory.ModifierLetter or
                UnicodeCategory.OtherLetter;

            var isDigit = category is
                UnicodeCategory.DecimalDigitNumber or
                UnicodeCategory.LetterNumber or
                UnicodeCategory.OtherNumber;

            var isPunctuation = category is
                UnicodeCategory.OpenPunctuation or
                UnicodeCategory.ClosePunctuation or
                UnicodeCategory.InitialQuotePunctuation or
                UnicodeCategory.FinalQuotePunctuation or
                UnicodeCategory.DashPunctuation or
                UnicodeCategory.ConnectorPunctuation or
                UnicodeCategory.OtherPunctuation;

            if (isLetter)
                letterCount++;
            else if (!isDigit && !char.IsWhiteSpace(ch) && !isPunctuation)
                junkCount++;
        }

        if (letterCount == 0)
            return false;

        if (text.Length > 0 && (double)junkCount / text.Length > 0.5)
            return false;

        return true;
    }
}
