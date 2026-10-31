namespace WindowGroups.Core.Models;

/// <summary>
/// A user-defined group of applications (matched by executable name) plus an
/// optional global keyboard shortcut used to minimise them.
/// </summary>
public sealed class ApplicationGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");

    public string Name { get; set; } = string.Empty;

    /// <summary>Executable names without the ".exe" extension.</summary>
    public List<string> ExecutableNames { get; set; } = new();

    public string? Description { get; set; }

    public Hotkey? Hotkey { get; set; }

    public bool Enabled { get; set; } = true;

    public ApplicationGroup Clone() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        Enabled = Enabled,
        Hotkey = Hotkey?.Clone(),
        ExecutableNames = new List<string>(ExecutableNames)
    };
}