using WindowGroups.Core.Interop;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;

namespace WindowGroups.Core.Services;

/// <summary>
/// Enumerates top-level windows and returns those belonging to the given
/// executable names that pass the user-facing window filters.
/// </summary>
public sealed class ProcessWindowEnumerator
{
    private readonly IWindowApi _windowApi;
    private readonly IProcessNameResolver _processNameResolver;
    private readonly int _excludedProcessId;
    private readonly ILogger _logger;

    public ProcessWindowEnumerator(
        IWindowApi windowApi,
        IProcessNameResolver processNameResolver,
        int excludedProcessId,
        ILogger logger)
    {
        _windowApi = windowApi ?? throw new ArgumentNullException(nameof(windowApi));
        _processNameResolver = processNameResolver ?? throw new ArgumentNullException(nameof(processNameResolver));
        _excludedProcessId = excludedProcessId;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<WindowInfo> EnumerateMatching(
        IReadOnlyCollection<string> executableNames,
        bool userFacingOnly = true)
    {
        var result = new List<WindowInfo>();

        foreach (var window in _windowApi.EnumerateTopLevelWindows())
        {
            // Never act on our own windows.
            if (window.ProcessId == _excludedProcessId)
            {
                continue;
            }

            try
            {
                if (userFacingOnly)
                {
                    var isVisible = _windowApi.IsWindowVisible(window.Handle);
                    var isToolWindow = _windowApi.HasToolWindowStyle(window.Handle);
                    var isCloaked = _windowApi.IsCloaked(window.Handle);

                    if (!WindowEligibility.IsUserFacing(isVisible, isToolWindow, isCloaked))
                    {
                        continue;
                    }
                }

                var processName = _processNameResolver.GetProcessName(window.ProcessId);
                if (!ExecutableMatcher.Matches(processName, executableNames))
                {
                    continue;
                }

                var title = _windowApi.GetWindowTitle(window.Handle);
                result.Add(new WindowInfo(window.Handle, window.ProcessId, processName, title));
            }
            catch (Exception ex)
            {
                _logger.Debug($"Skipping window handle 0x{window.Handle:X} due to an error: {ex.Message}");
            }
        }

        return result;
    }
}