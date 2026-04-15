using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Services;
using GhostLang.WPF.Services;

namespace GhostLang.WPF.Views;

public partial class TranslationOverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private CaptureRegion _region;

    public TranslationOverlayWindow(CaptureRegion region)
    {
        InitializeComponent();
        _region = region;
        PositionFromRegion();

        SourceInitialized += (_, _) =>
        {
            MakeClickThrough();
            WindowCaptureExclusion.ExcludeFromCapture(this);
        };
    }

    private void MakeClickThrough()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);
    }

    public void UpdateRegion(CaptureRegion region)
    {
        _region = region;
        PositionFromRegion();
    }

    private void PositionFromRegion()
    {
        var (scaleX, scaleY) = DpiHelper.GetDpiScale(this);

        Left = _region.X / scaleX;
        Top = _region.Y / scaleY;
        Width = Math.Max(1, _region.Width / scaleX);
        Height = Math.Max(1, _region.Height / scaleY);
    }

    public void RenderFrame(TranslationContext context)
    {
        OverlayCanvas.Children.Clear();

        if (context.TextFragments == null || context.TextFragments.Count == 0)
            return;

        var (scaleX, scaleY) = DpiHelper.GetDpiScale(this);

        foreach (var fragment in context.TextFragments)
        {
            if (fragment.RenderedPatch is not { Length: > 0 })
                continue;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(fragment.RenderedPatch);
            bitmap.EndInit();
            bitmap.Freeze();

            var image = new Image
            {
                Source = bitmap,
                Width = fragment.Bounds.Width / scaleX,
                Height = fragment.Bounds.Height / scaleY,
                Stretch = System.Windows.Media.Stretch.Fill
            };

            Canvas.SetLeft(image, fragment.Bounds.X / scaleX);
            Canvas.SetTop(image, fragment.Bounds.Y / scaleY);
            OverlayCanvas.Children.Add(image);
        }
    }

    public void HideOverlay() => Opacity = 0;

    public void ShowOverlay() => Opacity = 1;

    public void ClearOverlay()
    {
        OverlayCanvas.Children.Clear();
    }
}