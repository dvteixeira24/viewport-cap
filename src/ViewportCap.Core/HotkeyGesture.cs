namespace ViewportCap.Core;

[Flags]
public enum HotkeyModifiers : uint { None = 0, Alt = 1, Control = 2, Shift = 4, Win = 8 }

public readonly record struct HotkeyGesture(HotkeyModifiers Modifiers, uint VirtualKey)
{
    public static HotkeyGesture Default { get; } = new(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x52);
    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        var modifiers = HotkeyModifiers.None; uint key = 0;
        foreach (var raw in parts)
        {
            if (raw.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || raw.Equals("Control", StringComparison.OrdinalIgnoreCase)) { modifiers |= HotkeyModifiers.Control; continue; }
            if (raw.Equals("Alt", StringComparison.OrdinalIgnoreCase)) { modifiers |= HotkeyModifiers.Alt; continue; }
            if (raw.Equals("Shift", StringComparison.OrdinalIgnoreCase)) { modifiers |= HotkeyModifiers.Shift; continue; }
            if (raw.Equals("Win", StringComparison.OrdinalIgnoreCase) || raw.Equals("Windows", StringComparison.OrdinalIgnoreCase)) { modifiers |= HotkeyModifiers.Win; continue; }
            if (key != 0 || !KeyNames.TryGetValue(raw, out key)) return false;
        }
        if (key == 0 || modifiers == HotkeyModifiers.None) return false;
        gesture = new HotkeyGesture(modifiers, key); return true;
    }
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        var virtualKey = VirtualKey;
        parts.Add(KeyNames.FirstOrDefault(pair => pair.Value == virtualKey).Key ?? $"0x{virtualKey:X2}");
        return string.Join("+", parts);
    }
    private static readonly IReadOnlyDictionary<string, uint> KeyNames = CreateKeyNames();
    private static Dictionary<string, uint> CreateKeyNames()
    {
        var names = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        for (var c = 'A'; c <= 'Z'; c++) names[c.ToString()] = c;
        for (var c = '0'; c <= '9'; c++) names[c.ToString()] = c;
        for (var i = 1; i <= 12; i++) names[$"F{i}"] = (uint)(0x70 + i - 1);
        names["PrintScreen"] = 0x2C; names["Insert"] = 0x2D; names["Delete"] = 0x2E; names["Home"] = 0x24; names["End"] = 0x23;
        names["PageUp"] = 0x21; names["PageDown"] = 0x22; names["Space"] = 0x20; names["Tab"] = 0x09; names["Backspace"] = 0x08; names["Escape"] = 0x1B;
        return names;
    }
}
