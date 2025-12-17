using System.Drawing;
using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;

namespace GhostLang.Infrastructure.Services;

public class OcrService : IOcrService
{
    public Task<List<OcrBlock>> RecognizeTextAsync(Bitmap screenshot, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<OcrBlock>
        {
            new()
            {
                Text = "Здесь должен быть распознанный текст.",
                Bounds = new Rectangle(10, 10, 200, 50),
                Confidence = 0.95f
            }
        });
    }
}