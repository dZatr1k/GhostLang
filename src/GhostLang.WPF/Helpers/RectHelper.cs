using System.Drawing;
using System.Windows;

namespace GhostLang.WPF.Helpers;

public static class RectHelper
{
    public static Rect ToWpfRect(this Rectangle rectangle)
    {
        return new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    public static Rectangle ToDrawingRectangle(this Rect rect)
    {
        return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
    }
}