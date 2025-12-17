using System.Drawing;
using GhostLang.Application.Interfaces;

namespace GhostLang.Infrastructure.Services;

public class OcrService : IOcrService
{
    public Task<string> RecognizeTextAsync(Bitmap screenshot, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Fake Recognized text from image.");
    }
}