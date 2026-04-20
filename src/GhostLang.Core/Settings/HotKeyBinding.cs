namespace GhostLang.Core.Settings;

public class HotKeyBinding
{
    public string ActionId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string GroupKey { get; set; } = string.Empty;
    public uint Modifiers { get; set; }
    public uint Key { get; set; }

    public bool IsEmpty => Modifiers == 0 && Key == 0;

    public string ToDisplayString()
    {
        if (IsEmpty) return string.Empty;

        var parts = new List<string>();
        if ((Modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((Modifiers & 0x0004) != 0) parts.Add("Shift");
        if ((Modifiers & 0x0001) != 0) parts.Add("Alt");

        var keyName = Key switch
        {
            0x26 => "Up", 0x28 => "Down", 0x25 => "Left", 0x27 => "Right",
            >= 0x41 and <= 0x5A => ((char)Key).ToString(),
            >= 0x30 and <= 0x39 => ((char)Key).ToString(),
            0x20 => "Space", 0x1B => "Esc",
            _ => $"0x{Key:X2}"
        };
        parts.Add(keyName);
        return string.Join("+", parts);
    }
}
