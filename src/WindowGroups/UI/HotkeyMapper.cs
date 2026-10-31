using System.Windows.Forms;
using WindowGroups.Core.Models;

namespace WindowGroups.UI;

/// <summary>Converts WinForms key data into the core <see cref="Hotkey"/> model.</summary>
internal static class HotkeyMapper
{
    public static Hotkey? FromKeyData(Keys keyData)
    {
        var modifiers = HotkeyModifiers.None;
        if ((keyData & Keys.Control) == Keys.Control) modifiers |= HotkeyModifiers.Control;
        if ((keyData & Keys.Alt) == Keys.Alt) modifiers |= HotkeyModifiers.Alt;
        if ((keyData & Keys.Shift) == Keys.Shift) modifiers |= HotkeyModifiers.Shift;
        if ((keyData & Keys.LWin) == Keys.LWin || (keyData & Keys.RWin) == Keys.RWin) modifiers |= HotkeyModifiers.Windows;

        var key = keyData & Keys.KeyCode;

        if (key is Keys.None or Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin)
        {
            return null;
        }

        // Bare letter/digit keys would be far too intrusive as global hotkeys;
        // only allow standalone function keys without a modifier.
        if (modifiers == HotkeyModifiers.None && !IsStandaloneKeyAllowed(key))
        {
            return null;
        }

        return new Hotkey(modifiers, (uint)key);
    }

    private static bool IsStandaloneKeyAllowed(Keys key) =>
        key is >= Keys.F1 and <= Keys.F24;
}