using WindowGroups.Core.Interop;

namespace WindowGroups.Tests.TestDoubles;

internal sealed class FakeWindow
{
    public nint Handle { get; set; }
    public int ProcessId { get; set; }
    public string? Title { get; set; }
    public bool Visible { get; set; } = true;
    public bool ToolWindow { get; set; }
    public bool Cloaked { get; set; }
    public bool Iconic { get; set; }
    public bool Destroyed { get; set; }
}

/// <summary>In-memory <see cref="IWindowApi"/> for tests.</summary>
internal sealed class FakeWindowApi : IWindowApi
{
    public List<FakeWindow> Windows { get; } = new();
    public List<nint> Minimized { get; } = new();
    public List<nint> Restored { get; } = new();

    public IReadOnlyList<WindowHandleInfo> EnumerateTopLevelWindows() =>
        Windows.Where(w => !w.Destroyed).Select(w => new WindowHandleInfo(w.Handle, w.ProcessId)).ToList();

    public string? GetWindowTitle(nint handle) => Find(handle)?.Title;

    public bool IsWindow(nint handle) => Find(handle) is { } window && !window.Destroyed;

    public bool IsWindowVisible(nint handle) => Find(handle)?.Visible ?? false;

    public bool IsIconic(nint handle) => Find(handle)?.Iconic ?? false;

    public bool IsCloaked(nint handle) => Find(handle)?.Cloaked ?? false;

    public bool HasToolWindowStyle(nint handle) => Find(handle)?.ToolWindow ?? false;

    public void Minimize(nint handle)
    {
        Minimized.Add(handle);
        if (Find(handle) is { } window) window.Iconic = true;
    }

    public void Restore(nint handle)
    {
        Restored.Add(handle);
        if (Find(handle) is { } window) window.Iconic = false;
    }

    private FakeWindow? Find(nint handle) => Windows.FirstOrDefault(w => w.Handle == handle);

    public FakeWindow Add(int processId, string? title = null, nint? handle = null)
    {
        var window = new FakeWindow
        {
            Handle = handle ?? new nint(Windows.Count + 1),
            ProcessId = processId,
            Title = title
        };
        Windows.Add(window);
        return window;
    }
}