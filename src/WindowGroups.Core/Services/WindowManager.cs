using WindowGroups.Core.Interop;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;

namespace WindowGroups.Core.Services;

/// <summary>
/// Minimises and restores groups of windows. Minimisation targets visible,
/// user-facing windows; restoring returns every currently minimised window that
/// matches the configured executable names (regardless of whether this
/// application minimised it), so nothing is left hidden.
/// </summary>
public sealed class WindowManager
{
    private readonly IWindowApi _windowApi;
    private readonly ProcessWindowEnumerator _enumerator;
    private readonly ILogger _logger;

    public WindowManager(IWindowApi windowApi, ProcessWindowEnumerator enumerator, ILogger logger)
    {
        _windowApi = windowApi ?? throw new ArgumentNullException(nameof(windowApi));
        _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MinimizeResult MinimizeGroup(ApplicationGroup group)
    {
        if (group is null) throw new ArgumentNullException(nameof(group));
        if (!group.Enabled) return new MinimizeResult(group.Id, 0);

        var minimized = 0;

        foreach (var window in _enumerator.EnumerateMatching(group.ExecutableNames))
        {
            try
            {
                if (_windowApi.IsIconic(window.Handle))
                {
                    continue;
                }

                _windowApi.Minimize(window.Handle);
                minimized++;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to minimise window 0x{window.Handle:X}: {ex.Message}");
            }
        }

        return new MinimizeResult(group.Id, minimized);
    }

    public MinimizeResult MinimizeAll(IReadOnlyCollection<ApplicationGroup> groups)
    {
        if (groups is null) throw new ArgumentNullException(nameof(groups));

        var total = 0;
        foreach (var group in groups)
        {
            if (group is null || !group.Enabled)
            {
                continue;
            }

            total += MinimizeGroup(group).MinimizedCount;
        }

        return new MinimizeResult(string.Empty, total);
    }

    public RestoreResult RestoreGroup(ApplicationGroup group)
    {
        if (group is null) throw new ArgumentNullException(nameof(group));
        if (!group.Enabled) return new RestoreResult(group.Id, 0);

        return new RestoreResult(group.Id, RestoreMatches(group.ExecutableNames));
    }

    public RestoreResult RestoreAll(IReadOnlyCollection<ApplicationGroup> groups)
    {
        if (groups is null) throw new ArgumentNullException(nameof(groups));

        var executableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            if (group is null) continue;

            foreach (var name in group.ExecutableNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    executableNames.Add(name);
                }
            }
        }

        return new RestoreResult(string.Empty, RestoreMatches(executableNames));
    }

    /// <summary>
    /// Toggles a group: if any of its windows are currently minimised they are
    /// restored, otherwise its visible windows are minimised.
    /// </summary>
    public ToggleResult ToggleGroup(ApplicationGroup group)
    {
        if (group is null) throw new ArgumentNullException(nameof(group));
        if (!group.Enabled) return new ToggleResult(group.Id, Minimized: false, Count: 0);

        var anyMinimized = false;
        foreach (var window in _enumerator.EnumerateMatching(group.ExecutableNames, userFacingOnly: false))
        {
            if (_windowApi.IsIconic(window.Handle))
            {
                anyMinimized = true;
                break;
            }
        }

        if (anyMinimized)
        {
            return new ToggleResult(group.Id, Minimized: false, Count: RestoreMatches(group.ExecutableNames));
        }

        return new ToggleResult(group.Id, Minimized: true, Count: MinimizeGroup(group).MinimizedCount);
    }

    private int RestoreMatches(IReadOnlyCollection<string> executableNames)
    {
        var restored = 0;

        // Enumerate without the user-facing filters so minimised windows on other
        // virtual desktops (DWM-cloaked) are restored as well.
        foreach (var window in _enumerator.EnumerateMatching(executableNames, userFacingOnly: false))
        {
            try
            {
                if (!_windowApi.IsWindow(window.Handle) || !_windowApi.IsIconic(window.Handle))
                {
                    continue;
                }

                _windowApi.Restore(window.Handle);
                restored++;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to restore window 0x{window.Handle:X}: {ex.Message}");
            }
        }

        return restored;
    }
}