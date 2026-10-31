using WindowGroups.Core.Interop;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;

namespace WindowGroups.Core.Services;

/// <summary>
/// Registers and dispatches global keyboard shortcuts. Detection of conflicts
/// (hotkeys already registered by another application) surfaces as a
/// <see cref="HotkeyConflictException"/>.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int ErrorAlreadyRegistered = 1409;

    private readonly IHotkeyApi _hotkeyApi;
    private readonly ILogger _logger;
    private readonly Dictionary<int, Registration> _registrations = new();
    private int _nextId = 1000;
    private bool _disposed;

    public HotkeyManager(IHotkeyApi hotkeyApi, ILogger logger)
    {
        _hotkeyApi = hotkeyApi ?? throw new ArgumentNullException(nameof(hotkeyApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int Register(nint windowHandle, Hotkey hotkey, Action action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (hotkey is null) throw new ArgumentNullException(nameof(hotkey));
        if (action is null) throw new ArgumentNullException(nameof(action));

        var id = _nextId++;

        if (!_hotkeyApi.RegisterHotKey(windowHandle, id, (uint)hotkey.Modifiers, hotkey.Key))
        {
            var error = _hotkeyApi.GetLastError();
            if (error == ErrorAlreadyRegistered)
            {
                throw new HotkeyConflictException($"The shortcut '{hotkey}' is already in use by another application.");
            }

            throw new InvalidOperationException($"Failed to register hotkey '{hotkey}' (Win32 error {error}).");
        }

        _registrations[id] = new Registration(windowHandle, hotkey, action);
        return id;
    }

    public bool Unregister(int id)
    {
        if (!_registrations.Remove(id, out var registration))
        {
            return false;
        }

        try
        {
            return _hotkeyApi.UnregisterHotKey(registration.WindowHandle, id);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to unregister hotkey id {id}: {ex.Message}");
            return false;
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _registrations.Keys.ToArray())
        {
            Unregister(id);
        }
    }

    /// <summary>Invoked by the message window when a WM_HOTKEY message arrives.</summary>
    public bool TryHandleHotkey(int id)
    {
        if (!_registrations.TryGetValue(id, out var registration))
        {
            return false;
        }

        try
        {
            registration.Action();
        }
        catch (Exception ex)
        {
            _logger.Error($"Hotkey handler for id {id} threw an exception.", ex);
        }

        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        UnregisterAll();
        _disposed = true;
    }

    private sealed class Registration
    {
        public Registration(nint windowHandle, Hotkey hotkey, Action action)
        {
            WindowHandle = windowHandle;
            Hotkey = hotkey;
            Action = action;
        }

        public nint WindowHandle { get; }

        public Hotkey Hotkey { get; }

        public Action Action { get; }
    }
}