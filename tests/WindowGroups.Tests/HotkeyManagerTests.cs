using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;
using WindowGroups.Core.Services;
using WindowGroups.Tests.TestDoubles;
using Xunit;

namespace WindowGroups.Tests;

public sealed class HotkeyManagerTests
{
    [Fact]
    public void Register_ReturnsIncreasingIdsAndRecords()
    {
        var api = new FakeHotkeyApi();
        var manager = new HotkeyManager(api, NullLogger.Instance);
        var hotkey = new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x42);

        var first = manager.Register(1, hotkey, () => { });
        var second = manager.Register(1, new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43), () => { });

        Assert.True(second > first);
        Assert.Equal(2, api.Registered.Count);
    }

    [Fact]
    public void Register_ThrowsHotkeyConflict_WhenAlreadyRegistered()
    {
        var api = new FakeHotkeyApi { FailNextRegistration = true, LastError = 1409 };
        var manager = new HotkeyManager(api, NullLogger.Instance);

        var hotkey = new Hotkey(HotkeyModifiers.Control, 0x42);
        Assert.Throws<HotkeyConflictException>(() => manager.Register(1, hotkey, () => { }));
    }

    [Fact]
    public void Register_ThrowsInvalidOperation_ForOtherErrors()
    {
        var api = new FakeHotkeyApi { FailNextRegistration = true, LastError = 1300 };
        var manager = new HotkeyManager(api, NullLogger.Instance);

        var hotkey = new Hotkey(HotkeyModifiers.Control, 0x42);
        Assert.Throws<InvalidOperationException>(() => manager.Register(1, hotkey, () => { }));
    }

    [Fact]
    public void TryHandleHotkey_InvokesActionForKnownId()
    {
        var api = new FakeHotkeyApi();
        var manager = new HotkeyManager(api, NullLogger.Instance);
        var invoked = 0;

        var id = manager.Register(1, new Hotkey(HotkeyModifiers.Control, 0x42), () => invoked++);

        Assert.True(manager.TryHandleHotkey(id));
        Assert.Equal(1, invoked);
        Assert.False(manager.TryHandleHotkey(9999));
        Assert.Equal(1, invoked);
    }

    [Fact]
    public void UnregisterAll_UnregistersEveryHotkey()
    {
        var api = new FakeHotkeyApi();
        var manager = new HotkeyManager(api, NullLogger.Instance);

        manager.Register(1, new Hotkey(HotkeyModifiers.Control, 0x42), () => { });
        manager.Register(1, new Hotkey(HotkeyModifiers.Control, 0x43), () => { });

        manager.Dispose();

        Assert.Equal(2, api.Unregistered.Count);
    }
}