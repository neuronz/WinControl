namespace WindowGroups.Core.Services;

/// <summary>Thrown when a hotkey cannot be registered because it is already in use.</summary>
public sealed class HotkeyConflictException : Exception
{
    public HotkeyConflictException(string message) : base(message)
    {
    }
}