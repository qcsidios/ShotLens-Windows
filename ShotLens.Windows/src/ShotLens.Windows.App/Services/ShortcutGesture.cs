using System.Windows.Input;

namespace ShotLens.Windows.App.Services;

public sealed class ShortcutGesture
{
    public bool Control { get; init; }
    public bool Alt { get; init; }
    public bool Shift { get; init; }
    public bool Windows { get; init; }
    public string Key { get; init; } = "S";

    public static ShortcutGesture Default => new()
    {
        Control = true,
        Key = "S"
    };

    public static ShortcutGesture FromInput(Key key, ModifierKeys modifiers)
    {
        var normalizedKey = key.ToString();
        if (modifiers == ModifierKeys.None)
        {
            throw new InvalidOperationException("快捷键需要包含 Ctrl、Alt、Shift 或 Win。");
        }

        if (normalizedKey is "LeftCtrl" or "RightCtrl" or "LeftAlt" or "RightAlt" or "LeftShift" or "RightShift" or "LWin" or "RWin")
        {
            throw new InvalidOperationException("快捷键需要包含一个非修饰键。");
        }

        return new ShortcutGesture
        {
            Control = modifiers.HasFlag(ModifierKeys.Control),
            Alt = modifiers.HasFlag(ModifierKeys.Alt),
            Shift = modifiers.HasFlag(ModifierKeys.Shift),
            Windows = modifiers.HasFlag(ModifierKeys.Windows),
            Key = normalizedKey
        };
    }

    public Key ToWpfKey() => Enum.TryParse<Key>(Key, out var value) ? value : System.Windows.Input.Key.S;

    public string DisplayText
    {
        get
        {
            var parts = new List<string>();
            if (Control) parts.Add("Ctrl");
            if (Alt) parts.Add("Alt");
            if (Shift) parts.Add("Shift");
            if (Windows) parts.Add("Win");
            parts.Add(Key);
            return string.Join(" + ", parts);
        }
    }
}
