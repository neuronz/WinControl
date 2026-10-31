namespace WindowGroups.Core.Models;

/// <summary>
/// Immutable description of a top-level window that has been matched against a
/// group's executable names and passed the user-facing window filters.
/// </summary>
public sealed class WindowInfo
{
    public WindowInfo(nint handle, int processId, string? processName, string? title)
    {
        Handle = handle;
        ProcessId = processId;
        ProcessName = processName;
        Title = title;
    }

    public nint Handle { get; }

    public int ProcessId { get; }

    /// <summary>Executable name of the owning process (without ".exe").</summary>
    public string? ProcessName { get; }

    public string? Title { get; }
}