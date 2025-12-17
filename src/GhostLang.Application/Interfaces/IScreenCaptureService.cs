using System.Drawing;

namespace GhostLang.Application.Interfaces;

public interface IScreenCaptureService
{
    Bitmap CaptureScreenArea(RectangleF area);
}