using System.Runtime.InteropServices;
using System.Windows;

namespace GhostLang.WPF.Services;

public class MonitorInfo
{
    public int Index { get; set; }
    public bool IsPrimary { get; set; }
    public Rect Bounds { get; set; }
    public Rect WorkArea { get; set; }
}

public static class MonitorEnumeration
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    private const uint MONITORINFOF_PRIMARY = 1;

    public static List<MonitorInfo> EnumerateMonitors()
    {
        var list = new List<MonitorInfo>();

        try
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorCallback, IntPtr.Zero);

            bool MonitorCallback(IntPtr hMon, IntPtr hdc, ref RECT lprc, IntPtr data)
            {
                var mi = new MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(mi);

                if (GetMonitorInfo(hMon, ref mi))
                {
                    list.Add(new MonitorInfo
                    {
                        Index = list.Count,
                        IsPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0,
                        Bounds = new Rect(
                            mi.rcMonitor.Left, mi.rcMonitor.Top,
                            mi.rcMonitor.Right - mi.rcMonitor.Left,
                            mi.rcMonitor.Bottom - mi.rcMonitor.Top),
                        WorkArea = new Rect(
                            mi.rcWork.Left, mi.rcWork.Top,
                            mi.rcWork.Right - mi.rcWork.Left,
                            mi.rcWork.Bottom - mi.rcWork.Top)
                    });
                }
                return true;
            }
        }
        catch
        {
        }

        return list;
    }
}
