namespace WindowGroups.Core.Interop;

/// <summary>Resolves a process id to its executable name (without ".exe").</summary>
public interface IProcessNameResolver
{
    string? GetProcessName(int processId);
}