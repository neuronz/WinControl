using WindowGroups.Core.Services;
using Xunit;

namespace WindowGroups.Tests;

public sealed class ExecutableMatcherTests
{
    [Theory]
    [InlineData("chrome", "chrome")]
    [InlineData("CHROME", "chrome")]
    [InlineData("chrome", "Chrome")]
    [InlineData("chrome.exe", "chrome")]
    [InlineData("chrome", "chrome.exe")]
    [InlineData("  chrome  ", " chrome.EXE ")]
    public void Matches_IsCaseInsensitiveAndIgnoresExtension(string processName, string configured)
    {
        Assert.True(ExecutableMatcher.Matches(processName, new[] { configured }));
    }

    [Theory]
    [InlineData("firefox")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("chromet")]
    public void Matches_ReturnsFalse_ForNonMatchingNames(string? processName)
    {
        Assert.False(ExecutableMatcher.Matches(processName, new[] { "chrome", "msedge" }));
    }

    [Theory]
    [InlineData("chrome.exe", "chrome")]
    [InlineData("  chrome  ", "chrome")]
    [InlineData("CHROME.EXE", "CHROME")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void NormalizeExecutableName_BehavesAsExpected(string? input, string expected)
    {
        Assert.Equal(expected, ExecutableMatcher.NormalizeExecutableName(input));
    }
}