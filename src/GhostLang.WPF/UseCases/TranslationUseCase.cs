using System.Drawing;
using GhostLang.Application.Interfaces;

namespace GhostLang.WPF.UseCases;

public class TranslationUseCase(
    IOcrService ocrService,
    ITranslationService translationService,
    IGlossaryService glossaryService)
{
    private int _counter;
    public async Task<string> Translate(Bitmap screenshot, CancellationToken cancellationToken = default)
    {
        var originalText = await ocrService.RecognizeTextAsync(screenshot, cancellationToken);

        var translatedText = await translationService.TranslateAsync(originalText, cancellationToken);

        var finalResult = await glossaryService.ApplyGlossary(translatedText, [], cancellationToken);

        return $"{_counter++}: {finalResult}";
    }
}