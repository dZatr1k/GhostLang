using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace GhostLang.WPF.Services;

public static class DpiHelper
{
    public static (double ScaleX, double ScaleY) GetDpiScale(Visual visual)
    {
        var dpi = VisualTreeHelper.GetDpi(visual);
        return (dpi.DpiScaleX, dpi.DpiScaleY);
    }

    public static (int X, int Y) LogicalToPhysical(double logicalX, double logicalY, Visual visual)
    {
        var (scaleX, scaleY) = GetDpiScale(visual);
        return ((int)(logicalX * scaleX), (int)(logicalY * scaleY));
    }

    public static (double X, double Y) PhysicalToLogical(int physicalX, int physicalY, Visual visual)
    {
        var (scaleX, scaleY) = GetDpiScale(visual);
        return (physicalX / scaleX, physicalY / scaleY);
    }

    public static (int Width, int Height) LogicalSizeToPhysical(double logicalWidth, double logicalHeight, Visual visual)
    {
        var (scaleX, scaleY) = GetDpiScale(visual);
        return ((int)(logicalWidth * scaleX), (int)(logicalHeight * scaleY));
    }

    public static (int X, int Y) GetScreenPosition(Window window)
    {
        var (scaleX, scaleY) = GetDpiScale(window);
        return ((int)(window.Left * scaleX), (int)(window.Top * scaleY));
    }
}