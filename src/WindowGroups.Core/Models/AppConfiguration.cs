namespace WindowGroups.Core.Models;

/// <summary>Root configuration document persisted to disk.</summary>
public sealed class AppConfiguration
{
    public int Version { get; set; } = 1;

    public bool StartWithWindows { get; set; }

    public List<ApplicationGroup> Groups { get; set; } = new();
}