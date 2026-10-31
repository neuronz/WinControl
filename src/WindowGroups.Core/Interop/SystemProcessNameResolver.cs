using System.Diagnostics;

namespace WindowGroups.Core.Interop;

/// <summary>Resolves process names using <see cref="Process"/>. Returns null if the process no longer exists.</summary>
public sealed class SystemProcessNameResolver : IProcessNameResolver
{
    public string? GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }
}