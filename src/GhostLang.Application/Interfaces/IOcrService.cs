using System.Drawing;

namespace GhostLang.Application.Interfaces;

public interface IOcrService
{
    Task<string> RecognizeTextAsync(Bitmap screenshot, CancellationToken cancellationToken = default);
}