namespace WindowGroups.Core.Models;

/// <summary>
/// Modifier flags for a global hotkey. Values match the Win32 MOD_* constants
/// used by RegisterHotKey (MOD_ALT = 0x0001, MOD_CONTROL = 0x0002,
/// MOD_SHIFT = 0x0004, MOD_WIN = 0x0008).
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008
}