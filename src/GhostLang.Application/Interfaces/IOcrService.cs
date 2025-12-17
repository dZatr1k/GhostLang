using System.Drawing;
using GhostLang.Application.Models;

namespace GhostLang.Application.Interfaces;

public interface IOcrService
{
    Task<List<OcrBlock>> RecognizeTextAsync(Bitmap screenshot, CancellationToken cancellationToken = default);
}