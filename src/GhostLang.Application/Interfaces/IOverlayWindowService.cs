namespace GhostLang.Application.Interfaces;

public interface IOverlayWindowService
{
    void ShowSelectionDialog();
    void ShowOverlay();
    void CloseOverlay();
    void MoveOverlayX(double deltaX);
    void MoveOverlayY(double deltaY);
    void ResizeOverlayWidth(double deltaW);
    void ResizeOverlayHeight(double deltaH);
}