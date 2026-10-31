using WindowGroups.Core.Interop;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;

namespace WindowGroups.Core.Services;

/// <summary>
/// Minimises and restores groups of windows. Keeps an in-memory record of the
/// windows it has minimised so that restoring only affects windows previously
/// minimised by this application.
/// </summary>
public sealed class WindowManager
{
    private readonly IWindowApi _windowApi;
    private readonly ProcessWindowEnumerator _enumerator;
    private readonly ILogger _logger;
    private readonly Dictionary<string, HashSet<nint>> _minimized = new(StringComparer.Ordinal);
    private readonly object _sync = new();

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

                if (_windowApi.IsIconic(window.Handle))
                {
                    Track(group.Id, window.Handle);
                    minimized++;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to minimise window 0x{window.Handle:X}: {ex.Message}");
            }
        }

        return new MinimizeResult(group.Id, minimized);
    }

    public RestoreResult RestoreGroup(ApplicationGroup group)
    {
        if (group is null) throw new ArgumentNullException(nameof(group));
        if (!group.Enabled) return new RestoreResult(group.Id, 0);

        var restored = 0;

        lock (_sync)
        {
            if (!_minimized.TryGetValue(group.Id, out var handles))
            {
                return new RestoreResult(group.Id, 0);
            }

            foreach (var handle in handles.ToArray())
            {
                handles.Remove(handle);

                try
                {
                    if (!_windowApi.IsWindow(handle))
                    {
                        continue;
                    }

                    _windowApi.Restore(handle);
                    restored++;
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to restore window handle 0x{handle:X}: {ex.Message}");
                }
            }

            if (handles.Count == 0)
            {
                _minimized.Remove(group.Id);
            }
        }

        return new RestoreResult(group.Id, restored);
    }

    /// <summary>Snapshot of the handles recorded as minimised for a group. Primarily for testing.</summary>
    public IReadOnlyCollection<nint> GetMinimizedHandles(string groupId)
    {
        lock (_sync)
        {
            return _minimized.TryGetValue(groupId, out var handles)
                ? handles.ToArray()
                : Array.Empty<nint>();
        }
    }

    private void Track(string groupId, nint handle)
    {
        lock (_sync)
        {
            if (!_minimized.TryGetValue(groupId, out var handles))
            {
                handles = new HashSet<nint>();
                _minimized[groupId] = handles;
            }

            handles.Add(handle);
        }
    }
}