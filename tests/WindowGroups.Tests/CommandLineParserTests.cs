using WindowGroups.Core.Models;
using WindowGroups.Core.Services;
using Xunit;

namespace WindowGroups.Tests;

public sealed class CommandLineParserTests
{
    [Fact]
    public void Parse_EmptyOrNull_ReturnsNull()
    {
        Assert.Null(CommandLineParser.Parse(null));
        Assert.Null(CommandLineParser.Parse(Array.Empty<string>()));
    }

    [Theory]
    [InlineData("--settings")]
    [InlineData("--SETTINGS")]
    public void Parse_Settings_ReturnsShowSettings(string arg)
    {
        var result = CommandLineParser.Parse(new[] { arg });

        Assert.NotNull(result);
        Assert.Equal(CommandKind.ShowSettings, result!.Kind);
        Assert.Null(result.GroupId);
    }

    [Fact]
    public void Parse_Exit_ReturnsExit()
    {
        var result = CommandLineParser.Parse(new[] { "--exit" });

        Assert.NotNull(result);
        Assert.Equal(CommandKind.Exit, result!.Kind);
    }

    [Fact]
    public void Parse_Minimize_WithGroup_ReturnsMinimizeGroup()
    {
        var result = CommandLineParser.Parse(new[] { "--minimize", "browsers-default" });

        Assert.NotNull(result);
        Assert.Equal(CommandKind.MinimizeGroup, result!.Kind);
        Assert.Equal("browsers-default", result.GroupId);
    }

    [Fact]
    public void Parse_Restore_WithGroup_ReturnsRestoreGroup()
    {
        var result = CommandLineParser.Parse(new[] { "--restore", "  Browsers  " });

        Assert.NotNull(result);
        Assert.Equal(CommandKind.RestoreGroup, result!.Kind);
        Assert.Equal("Browsers", result.GroupId);
    }

    [Theory]
    [InlineData("--minimize")]
    [InlineData("--restore")]
    public void Parse_GroupCommandWithoutGroup_ReturnsNull(string arg)
    {
        Assert.Null(CommandLineParser.Parse(new[] { arg }));
    }

    [Fact]
    public void Parse_UnknownCommand_ReturnsNull()
    {
        Assert.Null(CommandLineParser.Parse(new[] { "--bogus" }));
    }
}