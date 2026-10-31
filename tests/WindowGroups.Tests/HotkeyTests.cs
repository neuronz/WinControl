using WindowGroups.Core.Models;
using Xunit;

namespace WindowGroups.Tests;

public sealed class HotkeyTests
{
    [Theory]
    [InlineData("Ctrl+Alt+B", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x42u, "Ctrl+Alt+B")]
    [InlineData("Ctrl+Alt+C", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43u, "Ctrl+Alt+C")]
    [InlineData("ctrl+shift+f1", HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x70u, "Ctrl+Shift+F1")]
    [InlineData("Win+Left", HotkeyModifiers.Windows, 0x25u, "Win+Left")]
    [InlineData("Alt+9", HotkeyModifiers.Alt, 0x39u, "Alt+9")]
    public void Parse_RoundTripsToFriendlyString(
        string input, HotkeyModifiers modifiers, uint key, string expected)
    {
        var hotkey = Hotkey.Parse(input);

        Assert.NotNull(hotkey);
        Assert.Equal(modifiers, hotkey!.Modifiers);
        Assert.Equal(key, hotkey.Key);
        Assert.Equal(expected, hotkey.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Bogus")]
    [InlineData("Bogus")]
    public void Parse_ReturnsNull_ForInvalidInput(string? input)
    {
        Assert.Null(Hotkey.Parse(input));
    }
}