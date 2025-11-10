using System.Drawing;
using System.Windows;

namespace GhostLang.WPF.Helpers;

public class RectHelper
{
    public static RectangleF ToDrawingRectangle(Rect rect)
    {
        return new RectangleF(
            (float)rect.X,
            (float)rect.Y,
            (float)rect.Width,
            (float)rect.Height);
    }
    
    public static Rect FromDrawingRectangle(RectangleF rectangle)
    {
        return new Rect(
            rectangle.X,
            rectangle.Y,
            rectangle.Width,
            rectangle.Height);
    }
    
    public static bool IsEmpty(Rect rect)
    {
        return rect.X == 0 && rect.Y == 0 && rect.Width == 0 && rect.Height == 0;
    }
}