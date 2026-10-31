using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;
using WindowGroups.Core.Services;
using WindowGroups.Tests.TestDoubles;
using Xunit;

namespace WindowGroups.Tests;

public sealed class WindowManagerTests
{
    private static (WindowManager Manager, FakeWindowApi Api) Create(int excludedPid = -1)
    {
        var api = new FakeWindowApi();
        var resolver = new FakeProcessNameResolver();
        resolver.Add(1, "chrome");

        var enumerator = new ProcessWindowEnumerator(api, resolver, excludedPid, NullLogger.Instance);
        return (new WindowManager(api, enumerator, NullLogger.Instance), api);
    }

    private static ApplicationGroup Browsers() => new()
    {
        Id = "browsers",
        Name = "Browsers",
        Enabled = true,
        ExecutableNames = { "chrome" }
    };

    [Fact]
    public void MinimizeGroup_MinimizesAndRecordsVisibleNonIconicWindows()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 1, Iconic = true }); // already minimized
        api.Windows.Add(new FakeWindow { Handle = 3, ProcessId = 1, Visible = false });

        var result = manager.MinimizeGroup(Browsers());

        Assert.Equal(1, result.MinimizedCount);
        Assert.Contains(api.Minimized, h => h == 1);
        Assert.Contains(manager.GetMinimizedHandles("browsers"), h => h == 1);
    }

    [Fact]
    public void MinimizeGroup_HandlesMultipleWindowsInSameProcess()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 1 });

        var result = manager.MinimizeGroup(Browsers());

        Assert.Equal(2, result.MinimizedCount);
        Assert.Equal(2, manager.GetMinimizedHandles("browsers").Count);
    }

    [Fact]
    public void MinimizeGroup_DoesNothing_ForDisabledGroup()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });

        var group = Browsers();
        group.Enabled = false;

        var result = manager.MinimizeGroup(group);

        Assert.Equal(0, result.MinimizedCount);
        Assert.Empty(api.Minimized);
    }

    [Fact]
    public void RestoreGroup_RestoresOnlyPreviouslyMinimizedWindows()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 1 });

        manager.MinimizeGroup(Browsers());
        var result = manager.RestoreGroup(Browsers());

        Assert.Equal(2, result.RestoredCount);
        Assert.Contains(api.Restored, h => h == 1);
        Assert.Contains(api.Restored, h => h == 2);
        Assert.Empty(manager.GetMinimizedHandles("browsers"));
    }

    [Fact]
    public void RestoreGroup_DoesNotRestoreWindowsItDidNotMinimize()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });

        var result = manager.RestoreGroup(Browsers());

        Assert.Equal(0, result.RestoredCount);
        Assert.Empty(api.Restored);
    }

    [Fact]
    public void RestoreGroup_SkipsDestroyedWindowHandles()
    {
        var (manager, api) = Create();
        var window = new FakeWindow { Handle = 1, ProcessId = 1 };
        api.Windows.Add(window);

        manager.MinimizeGroup(Browsers());
        window.Destroyed = true; // Window closed before restore.

        var result = manager.RestoreGroup(Browsers());

        Assert.Equal(0, result.RestoredCount);
        Assert.Empty(manager.GetMinimizedHandles("browsers"));
    }

    [Fact]
    public void RestoreGroup_DoesNothing_ForDisabledGroup()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });
        manager.MinimizeGroup(Browsers());

        var group = Browsers();
        group.Enabled = false;

        var result = manager.RestoreGroup(group);

        Assert.Equal(0, result.RestoredCount);
        Assert.Single(manager.GetMinimizedHandles("browsers"));
    }
}