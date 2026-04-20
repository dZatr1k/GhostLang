using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace GhostLang.WPF.Services;

public static class WindowCaptureExclusion
{
    private const uint WDA_NONE = 0x00000000;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    public static bool ExcludeFromCapture(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return false;
        return SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
    }

    public static void IncludeInCapture(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero)
            SetWindowDisplayAffinity(hwnd, WDA_NONE);
    }

    public static bool ExcludeFromCapture(Visual visual)
    {
        if (PresentationSource.FromVisual(visual) is not HwndSource source) return false;
        if (source.Handle == IntPtr.Zero) return false;
        return SetWindowDisplayAffinity(source.Handle, WDA_EXCLUDEFROMCAPTURE);
    }
}
