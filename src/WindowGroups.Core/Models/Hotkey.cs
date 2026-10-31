using System.Text;

namespace WindowGroups.Core.Models;

/// <summary>
/// A global keyboard shortcut, stored as modifier flags plus a Win32
/// virtual-key code. Serialised in configuration as a friendly string such as
/// "Ctrl+Alt+B".
/// </summary>
public sealed class Hotkey
{
    private const uint MinFunctionKey = 0x70; // VK_F1

    private static readonly Dictionary<string, uint> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Space"] = 0x20,
        ["Tab"] = 0x09,
        ["Enter"] = 0x0D,
        ["Escape"] = 0x1B,
        ["Esc"] = 0x1B,
        ["Backspace"] = 0x08,
        ["Delete"] = 0x2E,
        ["Insert"] = 0x2D,
        ["Home"] = 0x24,
        ["End"] = 0x23,
        ["PageUp"] = 0x21,
        ["PageDown"] = 0x22,
        ["Left"] = 0x25,
        ["Right"] = 0x27,
        ["Up"] = 0x26,
        ["Down"] = 0x28,
        ["CapsLock"] = 0x14,
        ["NumLock"] = 0x90,
        ["ScrollLock"] = 0x91,
        ["PrintScreen"] = 0x2C,
        ["Pause"] = 0x13,
        ["Oem1"] = 0xBA,
        ["OemPlus"] = 0xBB,
        ["OemComma"] = 0xBC,
        ["OemMinus"] = 0xBD,
        ["OemPeriod"] = 0xBE,
        ["Oem2"] = 0xBF,
        ["Oem3"] = 0xC0,
        ["Oem4"] = 0xDB,
        ["Oem5"] = 0xDC,
        ["Oem6"] = 0xDD,
        ["Oem7"] = 0xDE,
        ["Oem102"] = 0xE2
    };

    private static readonly Dictionary<uint, string> KeyNames = BuildKeyNames();

    public HotkeyModifiers Modifiers { get; set; }

    /// <summary>Win32 virtual-key code (e.g. 'B' = 0x42, F1 = 0x70).</summary>
    public uint Key { get; set; }

    public Hotkey()
    {
    }

    public Hotkey(HotkeyModifiers modifiers, uint key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    public bool HasModifiers => Modifiers != HotkeyModifiers.None;

    public override string ToString()
    {
        var sb = new StringBuilder();
        if ((Modifiers & HotkeyModifiers.Control) != 0) sb.Append("Ctrl+");
        if ((Modifiers & HotkeyModifiers.Alt) != 0) sb.Append("Alt+");
        if ((Modifiers & HotkeyModifiers.Shift) != 0) sb.Append("Shift+");
        if ((Modifiers & HotkeyModifiers.Windows) != 0) sb.Append("Win+");
        sb.Append(GetKeyName(Key));
        return sb.ToString();
    }

    public static Hotkey? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        var modifiers = HotkeyModifiers.None;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var parsed = ParseModifier(parts[i]);
            if (parsed is null) return null;
            modifiers |= parsed.Value;
        }

        return TryParseKey(parts[^1], out var key)
            ? new Hotkey(modifiers, key)
            : null;
    }

    public Hotkey Clone() => new() { Modifiers = Modifiers, Key = Key };

    public static string GetKeyName(uint keyCode) =>
        KeyNames.TryGetValue(keyCode, out var name) ? name : "VK" + keyCode;

    private static HotkeyModifiers? ParseModifier(string token) => token.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => HotkeyModifiers.Control,
        "ALT" => HotkeyModifiers.Alt,
        "SHIFT" => HotkeyModifiers.Shift,
        "WIN" or "WINDOWS" or "SUPER" or "CMD" => HotkeyModifiers.Windows,
        _ => null
    };

    private static bool TryParseKey(string token, out uint key)
    {
        key = 0;
        if (string.IsNullOrEmpty(token)) return false;

        if (token.Length == 1)
        {
            var c = token[0];
            if (c is >= 'A' and <= 'Z')
            {
                key = c;
                return true;
            }

            if (c is >= 'a' and <= 'z')
            {
                key = char.ToUpperInvariant(c);
                return true;
            }

            if (c is >= '0' and <= '9')
            {
                key = c;
                return true;
            }
        }

        if (token.Length >= 2
            && (token[0] == 'F' || token[0] == 'f')
            && int.TryParse(token.AsSpan(1), out var functionNumber)
            && functionNumber is >= 1 and <= 24)
        {
            key = MinFunctionKey + (uint)(functionNumber - 1);
            return true;
        }

        return NamedKeys.TryGetValue(token, out key);
    }

    private static Dictionary<uint, string> BuildKeyNames()
    {
        var map = new Dictionary<uint, string>();

        (uint Vk, string Name)[] named =
        {
            (0x20, "Space"), (0x09, "Tab"), (0x0D, "Enter"), (0x1B, "Escape"), (0x08, "Backspace"),
            (0x2E, "Delete"), (0x2D, "Insert"), (0x24, "Home"), (0x23, "End"), (0x21, "PageUp"),
            (0x22, "PageDown"), (0x25, "Left"), (0x27, "Right"), (0x26, "Up"), (0x28, "Down"),
            (0x14, "CapsLock"), (0x90, "NumLock"), (0x91, "ScrollLock"), (0x2C, "PrintScreen"), (0x13, "Pause"),
            (0xBA, "Oem1"), (0xBB, "OemPlus"), (0xBC, "OemComma"), (0xBD, "OemMinus"), (0xBE, "OemPeriod"),
            (0xBF, "Oem2"), (0xC0, "Oem3"), (0xDB, "Oem4"), (0xDC, "Oem5"), (0xDD, "Oem6"), (0xDE, "Oem7"), (0xE2, "Oem102")
        };
        foreach (var (vk, name) in named)
        {
            map[vk] = name;
        }

        for (uint c = 'A'; c <= 'Z'; c++) map[c] = ((char)c).ToString();
        for (uint c = '0'; c <= '9'; c++) map[c] = ((char)c).ToString();
        for (uint i = 0; i < 24; i++) map[MinFunctionKey + i] = "F" + (i + 1);

        return map;
    }
}