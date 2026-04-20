namespace GhostLang.Core.Services;

public interface IScreenCaptureService
{
    byte[] CaptureRegion(int x, int y, int width, int height);
}
