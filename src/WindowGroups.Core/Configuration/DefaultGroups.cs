using WindowGroups.Core.Models;

namespace WindowGroups.Core.Configuration;

/// <summary>Factory methods for the built-in groups.</summary>
public static class DefaultGroups
{
    public static ApplicationGroup CreateBrowsers() => new()
    {
        Id = WellKnownGroupIds.Browsers,
        Name = "Browsers",
        Description = "Web browsers",
        Enabled = true,
        Hotkey = new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x42), // Ctrl + Alt + B
        ExecutableNames = new List<string> { "chrome", "msedge", "firefox", "brave" }
    };

    public static ApplicationGroup CreateConsoles() => new()
    {
        Id = WellKnownGroupIds.Consoles,
        Name = "Consoles / Terminals",
        Description = "Console and terminal windows",
        Enabled = true,
        Hotkey = new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43), // Ctrl + Alt + C
        ExecutableNames = new List<string> { "WindowsTerminal", "cmd", "powershell", "pwsh" }
    };

    public static ApplicationGroup CreateWord() => new()
    {
        Id = WellKnownGroupIds.Word,
        Name = "Microsoft Word",
        Description = "Word document windows",
        Enabled = true,
        Hotkey = new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x57), // Ctrl + Alt + W
        ExecutableNames = new List<string> { "WINWORD" }
    };

    public static List<ApplicationGroup> CreateDefaultGroups() => new()
    {
        CreateBrowsers(),
        CreateConsoles(),
        CreateWord()
    };

    public static AppConfiguration CreateDefaultConfiguration() => new()
    {
        Version = 1,
        StartWithWindows = false,
        Groups = CreateDefaultGroups()
    };
}