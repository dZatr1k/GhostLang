using System.Drawing;
using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;

namespace GhostLang.WPF.UseCases;

public class TranslationUseCase(
    IOcrService ocrService,
    ITranslationService translationService,
    IGlossaryService glossaryService)
{
    public async Task<List<OcrBlock>> Translate(Bitmap screenshot, CancellationToken cancellationToken = default)
    {
        List<OcrBlock> blocks = await ocrService.RecognizeTextAsync(screenshot, cancellationToken);

        if (blocks.Count == 0) return blocks;

        var fullText = string.Join("\n", blocks.Select(b => b.Text));
        
        var translatedText = await translationService.TranslateAsync(fullText, cancellationToken);

        var finalResult = await glossaryService.ApplyGlossary(translatedText, [], cancellationToken);

        return blocks;
    }
}