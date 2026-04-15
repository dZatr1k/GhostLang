using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Settings;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class HotKeyBindingViewModel : ObservableObject
{
    [ObservableProperty] private string _actionId = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _keyDisplayString = string.Empty;
    [ObservableProperty] private bool _isRecording;

    public string DisplayNameKey { get; private set; } = string.Empty;
    public uint Modifiers { get; set; }
    public uint VirtualKey { get; set; }

    public void LoadFrom(HotKeyBinding binding)
    {
        ActionId = binding.ActionId;
        DisplayNameKey = binding.DisplayName;
        var loc = Services.LocalizationService.Instance;
        DisplayName = loc != null ? loc[DisplayNameKey] : DisplayNameKey;
        Modifiers = binding.Modifiers;
        VirtualKey = binding.Key;
        KeyDisplayString = binding.ToDisplayString();
    }

    public HotKeyBinding ToBinding() => new()
    {
        ActionId = ActionId,
        DisplayName = DisplayNameKey,
        Modifiers = Modifiers,
        Key = VirtualKey
    };

    [RelayCommand]
    private void StartRecording()
    {
        IsRecording = true;
        KeyDisplayString = Services.LocalizationService.Instance?["General_HotKeyPlaceholder"] ?? "Press keys...";
    }

    public void ApplyRecordedKey(Key key, ModifierKeys modifiers)
    {
        if (!IsRecording) return;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.System)
            return;

        uint mod = 0;
        if (modifiers.HasFlag(ModifierKeys.Control)) mod |= 0x0002;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mod |= 0x0004;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mod |= 0x0001;

        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        Modifiers = mod;
        VirtualKey = vk;
        KeyDisplayString = new HotKeyBinding { Modifiers = mod, Key = vk }.ToDisplayString();
        IsRecording = false;
    }

    public void CancelRecording()
    {
        if (!IsRecording) return;
        IsRecording = false;
        KeyDisplayString = new HotKeyBinding { Modifiers = Modifiers, Key = VirtualKey }.ToDisplayString();
    }
}