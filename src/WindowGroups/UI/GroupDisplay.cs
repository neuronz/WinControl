using WindowGroups.Core.Models;

namespace WindowGroups.UI;

/// <summary>Formats group names with their configured shortcut for menu items.</summary>
public static class GroupDisplay
{
    /// <summary>Appends the shortcut in parentheses when one is configured.</summary>
    public static string WithShortcut(string name, Hotkey? hotkey) =>
        hotkey is null ? name : $"{name}  ({hotkey})";
}