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
    public void MinimizeGroup_MinimizesVisibleNonIconicWindows()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 1, Iconic = true });
        api.Windows.Add(new FakeWindow { Handle = 3, ProcessId = 1, Visible = false });

        var result = manager.MinimizeGroup(Browsers());

        Assert.Equal(1, result.MinimizedCount);
        Assert.Single(api.Minimized);
        Assert.Contains(api.Minimized, h => h == 1);
    }

    [Fact]
    public void MinimizeGroup_HandlesMultipleWindowsInSameProcess()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 1 });

        var result = manager.MinimizeGroup(Browsers());

        Assert.Equal(2, result.MinimizedCount);
        Assert.Equal(2, api.Minimized.Count);
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
    public void RestoreGroup_RestoresEveryMinimizedMatchingWindow()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1, Iconic = true });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 1 });

        var result = manager.RestoreGroup(Browsers());

        Assert.Equal(1, result.RestoredCount);
        Assert.Contains(api.Restored, h => h == 1);
        Assert.DoesNotContain(api.Restored, h => h == 2);
    }

    [Fact]
    public void RestoreGroup_SkipsDestroyedWindowHandles()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1, Iconic = true, Destroyed = true });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 1, Iconic = true });

        var result = manager.RestoreGroup(Browsers());

        Assert.Equal(1, result.RestoredCount);
        Assert.Contains(api.Restored, h => h == 2);
    }

    [Fact]
    public void RestoreGroup_DoesNothing_ForDisabledGroup()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1, Iconic = true });

        var group = Browsers();
        group.Enabled = false;

        var result = manager.RestoreGroup(group);

        Assert.Equal(0, result.RestoredCount);
        Assert.Empty(api.Restored);
    }

    [Fact]
    public void MinimizeAll_MinimizesAcrossEnabledGroups()
    {
        var api = new FakeWindowApi();
        var resolver = new FakeProcessNameResolver();
        resolver.Add(1, "chrome");
        resolver.Add(2, "winword");
        var enumerator = new ProcessWindowEnumerator(api, resolver, -1, NullLogger.Instance);
        var manager = new WindowManager(api, enumerator, NullLogger.Instance);

        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 2 });

        var browsers = new ApplicationGroup { Id = "b", Name = "Browsers", Enabled = true, ExecutableNames = { "chrome" } };
        var word = new ApplicationGroup { Id = "w", Name = "Word", Enabled = true, ExecutableNames = { "winword" } };
        var disabled = new ApplicationGroup { Id = "d", Name = "Disabled", Enabled = false, ExecutableNames = { "notepad" } };

        var result = manager.MinimizeAll(new List<ApplicationGroup> { browsers, word, disabled });

        Assert.Equal(2, result.MinimizedCount);
        Assert.Equal(2, api.Minimized.Count);
    }

    [Fact]
    public void RestoreAll_RestoresEveryMinimizedMatchingWindowAcrossGroups()
    {
        var api = new FakeWindowApi();
        var resolver = new FakeProcessNameResolver();
        resolver.Add(1, "chrome");
        resolver.Add(2, "winword");
        resolver.Add(3, "notepad");
        var enumerator = new ProcessWindowEnumerator(api, resolver, -1, NullLogger.Instance);
        var manager = new WindowManager(api, enumerator, NullLogger.Instance);

        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1, Iconic = true });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 2, Iconic = true });
        api.Windows.Add(new FakeWindow { Handle = 3, ProcessId = 3, Iconic = true });
        api.Windows.Add(new FakeWindow { Handle = 4, ProcessId = 1 });

        var browsers = new ApplicationGroup { Id = "b", Name = "Browsers", Enabled = true, ExecutableNames = { "chrome" } };
        var word = new ApplicationGroup { Id = "w", Name = "Word", Enabled = true, ExecutableNames = { "winword" } };
        var disabled = new ApplicationGroup { Id = "d", Name = "Disabled", Enabled = false, ExecutableNames = { "notepad" } };

        var result = manager.RestoreAll(new List<ApplicationGroup> { browsers, word, disabled });

        Assert.Equal(3, result.RestoredCount);
        Assert.Contains(api.Restored, h => h == 1);
        Assert.Contains(api.Restored, h => h == 2);
        Assert.Contains(api.Restored, h => h == 3);
        Assert.DoesNotContain(api.Restored, h => h == 4);
    }

    [Fact]
    public void RestoreAll_RestoresCloakedMinimizedWindows()
    {
        var api = new FakeWindowApi();
        var resolver = new FakeProcessNameResolver();
        resolver.Add(1, "chrome");
        var enumerator = new ProcessWindowEnumerator(api, resolver, -1, NullLogger.Instance);
        var manager = new WindowManager(api, enumerator, NullLogger.Instance);

        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1, Iconic = true, Cloaked = true });

        var result = manager.RestoreAll(new List<ApplicationGroup> { Browsers() });

        Assert.Equal(1, result.RestoredCount);
        Assert.Contains(api.Restored, h => h == 1);
    }

    [Fact]
    public void ToggleGroup_MinimizesWhenNothingIsMinimized()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });

        var result = manager.ToggleGroup(Browsers());

        Assert.True(result.Minimized);
        Assert.Equal(1, result.Count);
        Assert.Contains(api.Minimized, h => h == 1);
        Assert.Empty(api.Restored);
    }

    [Fact]
    public void ToggleGroup_RestoresWhenAlreadyMinimized()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1, Iconic = true });

        var result = manager.ToggleGroup(Browsers());

        Assert.False(result.Minimized);
        Assert.Equal(1, result.Count);
        Assert.Contains(api.Restored, h => h == 1);
        Assert.Empty(api.Minimized);
    }

    [Fact]
    public void ToggleGroup_DoesNothing_ForDisabledGroup()
    {
        var (manager, api) = Create();
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1 });

        var group = Browsers();
        group.Enabled = false;

        var result = manager.ToggleGroup(group);

        Assert.False(result.Minimized);
        Assert.Equal(0, result.Count);
        Assert.Empty(api.Minimized);
        Assert.Empty(api.Restored);
    }
}