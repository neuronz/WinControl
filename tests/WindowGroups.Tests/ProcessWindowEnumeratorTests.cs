using WindowGroups.Core.Logging;
using WindowGroups.Core.Services;
using WindowGroups.Tests.TestDoubles;
using Xunit;

namespace WindowGroups.Tests;

public sealed class ProcessWindowEnumeratorTests
{
    private static ProcessWindowEnumerator Create(FakeWindowApi api, FakeProcessNameResolver resolver, int excludedPid = -1) =>
        new(api, resolver, excludedPid, NullLogger.Instance);

    [Fact]
    public void EnumerateMatching_ReturnsOnlyMatchingUserFacingWindows()
    {
        var api = new FakeWindowApi();
        var resolver = new FakeProcessNameResolver();
        resolver.Add(1, "chrome");
        resolver.Add(2, "chrome");
        resolver.Add(3, "firefox");

        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 1, Title = "A" });
        api.Windows.Add(new FakeWindow { Handle = 2, ProcessId = 2, Title = "B" }); // second process, same exe
        api.Windows.Add(new FakeWindow { Handle = 3, ProcessId = 3, Title = "C" }); // different exe
        api.Windows.Add(new FakeWindow { Handle = 4, ProcessId = 1, Title = "Hidden", Visible = false });
        api.Windows.Add(new FakeWindow { Handle = 5, ProcessId = 1, Title = "Tool", ToolWindow = true });
        api.Windows.Add(new FakeWindow { Handle = 6, ProcessId = 1, Title = "Cloaked", Cloaked = true });

        var enumerator = Create(api, resolver);
        var result = enumerator.EnumerateMatching(new[] { "chrome" });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.Handle == 1);
        Assert.Contains(result, w => w.Handle == 2);
    }

    [Fact]
    public void EnumerateMatching_ExcludesOwnProcess()
    {
        var api = new FakeWindowApi();
        var resolver = new FakeProcessNameResolver();
        resolver.Add(10, "chrome");
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 10 });

        var enumerator = Create(api, resolver, excludedPid: 10);

        Assert.Empty(enumerator.EnumerateMatching(new[] { "chrome" }));
    }

    [Fact]
    public void EnumerateMatching_ToleratesDisappearingProcesses()
    {
        var api = new FakeWindowApi();
        var resolver = new FakeProcessNameResolver();
        // Process 42 has no entry in the resolver (simulates the process having exited).
        api.Windows.Add(new FakeWindow { Handle = 1, ProcessId = 42 });

        var result = Create(api, resolver).EnumerateMatching(new[] { "chrome" });

        Assert.Empty(result);
    }
}