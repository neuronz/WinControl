namespace WindowGroups.Core.Models;

public enum CommandKind
{
    ShowSettings,
    Exit,
    MinimizeGroup,
    RestoreGroup
}

/// <summary>
/// A command sent to the running application instance, either from a second
/// launch or from a taskbar jump-list task.
/// </summary>
public sealed class CommandRequest
{
    public CommandRequest()
    {
    }

    public CommandRequest(CommandKind kind, string? groupId)
    {
        Kind = kind;
        GroupId = groupId;
    }

    public CommandKind Kind { get; set; }

    /// <summary>Group id or name; used for MinimizeGroup/RestoreGroup commands.</summary>
    public string? GroupId { get; set; }
}