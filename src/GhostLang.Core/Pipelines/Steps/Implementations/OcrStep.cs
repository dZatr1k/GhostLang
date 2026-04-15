using GhostLang.Core.Pipelines.Models;
using GhostLang.Core.Settings.Ocr;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class OcrStep(IOcrEngine ocrEngine) : IMandatoryPipelineStep
{
    public string StepName => "Text Recognition (OCR)";

    public async Task ExecuteAsync(TranslationContext context)
    {
        if (context.IsAborted || context.ProcessedImage == null)
            return;

        var fragments = await ocrEngine.RecognizeTextAsync(context, context.SourceLanguage);

        context.TextFragments.AddRange(fragments.Where(IsValidFragment));
    }

    private static bool IsValidFragment(TextFragment fragment)
    {
        var text = fragment.OriginalText;

        // Too short
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
            return false;

        // Bounding box too small
        if (fragment.Bounds.Width < 5 || fragment.Bounds.Height < 5)
            return false;

        // Count letters vs junk
        var letterCount = 0;
        var junkCount = 0;

        foreach (var ch in text)
        {
            if (char.IsLetter(ch))
                letterCount++;
            else if (!char.IsDigit(ch) && !char.IsWhiteSpace(ch) && ch is not ('.' or ',' or '!' or '?' or '-' or ':' or ';' or '\'' or '"'))
                junkCount++;
        }

        // No letters at all
        if (letterCount == 0)
            return false;

        // More than 50% junk symbols
        if (text.Length > 0 && (double)junkCount / text.Length > 0.5)
            return false;

        return true;
    }
}