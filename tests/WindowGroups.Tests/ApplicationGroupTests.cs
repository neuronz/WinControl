using WindowGroups.Core.Configuration;
using WindowGroups.Core.Models;
using Xunit;

namespace WindowGroups.Tests;

public sealed class ApplicationGroupTests
{
    [Fact]
    public void Clone_ProducesIndependentCopy()
    {
        var group = new ApplicationGroup
        {
            Name = "X",
            Hotkey = new Hotkey(HotkeyModifiers.Control, 0x42),
            ExecutableNames = { "a" }
        };

        var clone = group.Clone();
        clone.Name = "Y";
        clone.ExecutableNames.Add("b");
        clone.Hotkey!.Key = 0x43;
        clone.Hotkey.Modifiers = HotkeyModifiers.None;

        Assert.Equal("X", group.Name);
        Assert.Single(group.ExecutableNames);
        Assert.Equal(0x42u, group.Hotkey!.Key);
        Assert.Equal(HotkeyModifiers.Control, group.Hotkey.Modifiers);
    }

    [Fact]
    public void DefaultGroups_HaveExpectedExecutablesAndHotkeys()
    {
        var browsers = DefaultGroups.CreateBrowsers();
        Assert.Equal(new[] { "chrome", "msedge", "firefox", "brave" }, browsers.ExecutableNames);
        Assert.Equal("Ctrl+Alt+B", browsers.Hotkey!.ToString());
        Assert.True(browsers.Enabled);

        var consoles = DefaultGroups.CreateConsoles();
        Assert.Equal(new[] { "WindowsTerminal", "cmd", "powershell", "pwsh" }, consoles.ExecutableNames);
        Assert.Equal("Ctrl+Alt+C", consoles.Hotkey!.ToString());

        var word = DefaultGroups.CreateWord();
        Assert.Equal(new[] { "WINWORD" }, word.ExecutableNames);
        Assert.Equal("Ctrl+Alt+W", word.Hotkey!.ToString());
        Assert.True(word.Enabled);
    }
}