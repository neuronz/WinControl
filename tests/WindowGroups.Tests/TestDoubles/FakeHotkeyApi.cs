using WindowGroups.Core.Interop;

namespace WindowGroups.Tests.TestDoubles;

/// <summary>In-memory <see cref="IHotkeyApi"/> for tests.</summary>
internal sealed class FakeHotkeyApi : IHotkeyApi
{
    public List<(nint Hwnd, int Id, uint Modifiers, uint Key)> Registered { get; } = new();
    public List<int> Unregistered { get; } = new();

    public bool FailNextRegistration { get; set; }

    public int LastError { get; set; } = 1409;

    public bool RegisterHotKey(nint windowHandle, int id, uint modifiers, uint keyCode)
    {
        if (FailNextRegistration)
        {
            FailNextRegistration = false;
            return false;
        }

        Registered.Add((windowHandle, id, modifiers, keyCode));
        return true;
    }

    public bool UnregisterHotKey(nint windowHandle, int id)
    {
        Unregistered.Add(id);
        return true;
    }

    public int GetLastError() => LastError;
}