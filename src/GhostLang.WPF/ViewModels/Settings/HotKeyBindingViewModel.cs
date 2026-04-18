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
    [ObservableProperty] private string _groupDisplayName = string.Empty;
    [ObservableProperty] private bool _hasBinding;

    public string DisplayNameKey { get; private set; } = string.Empty;
    public string GroupKey { get; private set; } = string.Empty;
    public uint Modifiers { get; set; }
    public uint VirtualKey { get; set; }

    public void LoadFrom(HotKeyBinding binding)
    {
        ActionId = binding.ActionId;
        DisplayNameKey = binding.DisplayName;
        GroupKey = binding.GroupKey;
        var loc = Services.LocalizationService.Instance;
        DisplayName = loc != null ? loc[DisplayNameKey] : DisplayNameKey;
        GroupDisplayName = loc != null && !string.IsNullOrEmpty(GroupKey) ? loc[GroupKey] : GroupKey;
        Modifiers = binding.Modifiers;
        VirtualKey = binding.Key;
        KeyDisplayString = binding.ToDisplayString();
        HasBinding = !binding.IsEmpty;
    }

    public HotKeyBinding ToBinding() => new()
    {
        ActionId = ActionId,
        DisplayName = DisplayNameKey,
        GroupKey = GroupKey,
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

        if (key == Key.Escape)
        {
            ClearBinding();
            return;
        }

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
        HasBinding = true;
        IsRecording = false;
    }

    [RelayCommand]
    private void ClearBinding()
    {
        Modifiers = 0;
        VirtualKey = 0;
        KeyDisplayString = string.Empty;
        HasBinding = false;
        IsRecording = false;
    }

    public void CancelRecording()
    {
        if (!IsRecording) return;
        IsRecording = false;
        KeyDisplayString = new HotKeyBinding { Modifiers = Modifiers, Key = VirtualKey }.ToDisplayString();
    }
}