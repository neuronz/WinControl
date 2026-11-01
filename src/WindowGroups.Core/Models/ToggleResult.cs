namespace WindowGroups.Core.Models;

/// <summary>
/// Result of toggling a group: whether the action minimised or restored the
/// windows, and how many were affected.
/// </summary>
public sealed record ToggleResult(string GroupId, bool Minimized, int Count);