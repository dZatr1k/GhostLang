using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace GhostLang.WPF.Services;

public class GlobalHotkeyService : IDisposable
{
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _currentId;

    private const int WM_HOTKEY = 0x0312;

    public GlobalHotkeyService()
    {
        ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;
    }

    public void Register(ModifierKeys modifiers, Key key, Action action)
    {
        var virtualKeyCode = KeyInterop.VirtualKeyFromKey(key);
        _currentId++;

        var result = RegisterHotKey(IntPtr.Zero, _currentId, (uint)modifiers, (uint)virtualKeyCode);

        if (!result)
        {
            throw new InvalidOperationException($"Не удалось зарегистрировать хоткей: {modifiers} + {key}. Возможно, он занят другой программой.");
        }

        _hotkeyActions.Add(_currentId, action);
    }

    private void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message == WM_HOTKEY)
        {
            var id = (int)msg.wParam;

            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action.Invoke();
                handled = true;
            }
        }
    }

    public void Dispose()
    {
        foreach (var id in _hotkeyActions.Keys)
        {
            UnregisterHotKey(IntPtr.Zero, id);
        }
        ComponentDispatcher.ThreadFilterMessage -= ComponentDispatcher_ThreadFilterMessage;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}