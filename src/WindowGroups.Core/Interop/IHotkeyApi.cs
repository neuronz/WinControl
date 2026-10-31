namespace WindowGroups.Core.Interop;

/// <summary>Abstraction over RegisterHotKey/UnregisterHotKey for testability.</summary>
public interface IHotkeyApi
{
    bool RegisterHotKey(nint windowHandle, int id, uint modifiers, uint keyCode);

    bool UnregisterHotKey(nint windowHandle, int id);

    int GetLastError();
}