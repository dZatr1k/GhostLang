using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using GhostLang.Core.Services;
using GhostLang.Core.Settings;

namespace GhostLang.WPF.Services;

public class GlobalHotKeyService(IConfigurationService configService) : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _hwnd;
    private HwndSource? _source;
    private readonly Dictionary<int, string> _idToAction = new();
    private int _nextId = 9001;

    public event Action? SelectRegionRequested;
    public event Action? ToggleVisibility;
    public event Action<int, int>? MoveRequested;
    public event Action<int, int>? ResizeRequested;
    public event Action? BindingsReloaded;
    public event Action? StartStopAudioRequested;
    public event Action? ToggleSubtitleVisibilityRequested;

    public void Register(Window window)
    {
        _hwnd = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);

        ReloadBindings();
    }

    public void ReloadBindings()
    {
        UnregisterAll();

        var config = configService.Load();
        foreach (var binding in config.HotKeys)
        {
            if (binding.IsEmpty) continue;
            var id = _nextId++;
            if (RegisterHotKey(_hwnd, id, binding.Modifiers, binding.Key))
                _idToAction[id] = binding.ActionId;
        }

        BindingsReloaded?.Invoke();
    }

    private void UnregisterAll()
    {
        foreach (var id in _idToAction.Keys)
            UnregisterHotKey(_hwnd, id);
        _idToAction.Clear();
    }

    public void Unregister()
    {
        UnregisterAll();
        _source?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg != WM_HOTKEY) return IntPtr.Zero;

        var id = (int)wParam;
        if (!_idToAction.TryGetValue(id, out var action)) return IntPtr.Zero;

        const int step = 10;
        handled = true;

        switch (action)
        {
            case "select_region": SelectRegionRequested?.Invoke(); break;
            case "toggle_visibility": ToggleVisibility?.Invoke(); break;
            case "move_up": MoveRequested?.Invoke(0, -step); break;
            case "move_down": MoveRequested?.Invoke(0, step); break;
            case "move_left": MoveRequested?.Invoke(-step, 0); break;
            case "move_right": MoveRequested?.Invoke(step, 0); break;
            case "resize_up": ResizeRequested?.Invoke(0, -step); break;
            case "resize_down": ResizeRequested?.Invoke(0, step); break;
            case "resize_left": ResizeRequested?.Invoke(-step, 0); break;
            case "resize_right": ResizeRequested?.Invoke(step, 0); break;
            case "start_stop_audio": StartStopAudioRequested?.Invoke(); break;
            case "toggle_subtitle_visibility": ToggleSubtitleVisibilityRequested?.Invoke(); break;
            default: handled = false; break;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
    }
}