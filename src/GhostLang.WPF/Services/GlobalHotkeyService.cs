using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using GhostLang.Core.Services;
using GhostLang.Core.Settings;

namespace GhostLang.WPF.Services;

public enum HotKeyConflictKind
{
    DuplicateInApp,
    SystemOwned
}

public record HotKeyConflict(string ActionId, string Combination, HotKeyConflictKind Kind);

public class HotKeyReloadResult
{
    public List<HotKeyConflict> Conflicts { get; } = new();
    public bool HasConflicts => Conflicts.Count > 0;
}

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
    private bool _isSuspended;

    public HotKeyReloadResult LastReloadResult { get; private set; } = new();

    public event Action? SelectRegionRequested;
    public event Action? ToggleVisibility;
    public event Action<int, int>? MoveRequested;
    public event Action<int, int>? ResizeRequested;
    public event Action<HotKeyReloadResult>? BindingsReloaded;
    public event Action? StartStopAudioRequested;
    public event Action? ToggleSubtitleVisibilityRequested;
    public event Action? ScreenStartRequested;
    public event Action? ScreenStopRequested;

    public void Register(Window window)
    {
        _hwnd = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);

        ReloadBindings();
    }

    public HotKeyReloadResult ReloadBindings()
    {
        UnregisterAll();

        var result = new HotKeyReloadResult();

        if (_hwnd == IntPtr.Zero)
        {
            LastReloadResult = result;
            BindingsReloaded?.Invoke(result);
            return result;
        }

        var config = configService.Load();
        var nonEmpty = config.HotKeys.Where(b => !b.IsEmpty).ToList();

        var duplicates = new HashSet<HotKeyBinding>();
        foreach (var group in nonEmpty.GroupBy(b => (b.Modifiers, b.Key)).Where(g => g.Count() > 1))
        {
            foreach (var member in group)
            {
                duplicates.Add(member);
                result.Conflicts.Add(new HotKeyConflict(member.ActionId, member.ToDisplayString(), HotKeyConflictKind.DuplicateInApp));
            }
        }

        if (!_isSuspended)
        {
            foreach (var binding in nonEmpty)
            {
                if (duplicates.Contains(binding)) continue;

                var id = _nextId++;
                if (RegisterHotKey(_hwnd, id, binding.Modifiers, binding.Key))
                {
                    _idToAction[id] = binding.ActionId;
                }
                else
                {
                    result.Conflicts.Add(new HotKeyConflict(binding.ActionId, binding.ToDisplayString(), HotKeyConflictKind.SystemOwned));
                }
            }
        }

        LastReloadResult = result;
        BindingsReloaded?.Invoke(result);
        return result;
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

    public void SuspendBindings()
    {
        _isSuspended = true;
        UnregisterAll();
    }

    public void ResumeBindings()
    {
        _isSuspended = false;
        ReloadBindings();
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
            case "screen_start": ScreenStartRequested?.Invoke(); break;
            case "screen_stop": ScreenStopRequested?.Invoke(); break;
            default: handled = false; break;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
    }
}
