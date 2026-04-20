using System.Windows.Media.Imaging;

namespace GhostLang.WPF.ViewModels;

public class RenderedFragmentViewModel
{
    public BitmapSource Image { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
