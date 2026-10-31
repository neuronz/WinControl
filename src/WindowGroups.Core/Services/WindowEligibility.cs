namespace WindowGroups.Core.Services;

/// <summary>
/// Pure predicate deciding whether a window should be treated as user-facing.
/// Invisible, tool and DWM-cloaked windows are all filtered out.
/// </summary>
public static class WindowEligibility
{
    public static bool IsUserFacing(bool isVisible, bool isToolWindow, bool isCloaked)
        => isVisible && !isToolWindow && !isCloaked;
}