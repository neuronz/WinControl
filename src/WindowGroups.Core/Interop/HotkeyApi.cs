using System.Runtime.InteropServices;

namespace WindowGroups.Core.Interop;

/// <summary>Production <see cref="IHotkeyApi"/> backed by Win32.</summary>
public sealed class HotkeyApi : IHotkeyApi
{
    public bool RegisterHotKey(nint windowHandle, int id, uint modifiers, uint keyCode) =>
        NativeMethods.RegisterHotKey(windowHandle, id, modifiers, keyCode);

    public bool UnregisterHotKey(nint windowHandle, int id) =>
        NativeMethods.UnregisterHotKey(windowHandle, id);

    public int GetLastError() => Marshal.GetLastWin32Error();
}