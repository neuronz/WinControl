namespace WindowGroups.Core.Interop;

/// <summary>A top-level window handle and its owning process id.</summary>
public readonly record struct WindowHandleInfo(nint Handle, int ProcessId);

/// <summary>
/// Abstracts the Win32 window operations the application needs so that the
/// window-management logic can be unit tested without a real desktop.
/// </summary>
public interface IWindowApi
{
    IReadOnlyList<WindowHandleInfo> EnumerateTopLevelWindows();

    string? GetWindowTitle(nint handle);

    bool IsWindow(nint handle);

    bool IsWindowVisible(nint handle);

    bool IsIconic(nint handle);

    bool IsCloaked(nint handle);

    bool HasToolWindowStyle(nint handle);

    void Minimize(nint handle);

    void Restore(nint handle);
}