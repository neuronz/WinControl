using WindowGroups.Core.Configuration;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;
using Xunit;

namespace WindowGroups.Tests;

public sealed class ConfigurationServiceTests
{
    [Fact]
    public void Load_WhenFileMissing_ReturnsDefaultConfiguration()
    {
        var path = TestPath.New();
        try
        {
            var service = new ConfigurationService(path, NullLogger.Instance);
            var config = service.Load();

            Assert.Equal(3, config.Groups.Count);
            Assert.Contains(config.Groups, g => g.Name == "Browsers");
            Assert.Contains(config.Groups, g => g.Name == "Consoles / Terminals");
            Assert.Contains(config.Groups, g => g.Name == "Microsoft Word");
        }
        finally
        {
            TestPath.Clean(path);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsGroupsAndHotkeys()
    {
        var path = TestPath.New();
        try
        {
            var service = new ConfigurationService(path, NullLogger.Instance);
            var config = new AppConfiguration
            {
                StartWithWindows = true,
                Groups =
                {
                    new ApplicationGroup
                    {
                        Id = "g1",
                        Name = "Editors",
                        Description = "Text editors",
                        Hotkey = new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x45),
                        ExecutableNames = { "notepad", "code" }
                    }
                }
            };

            service.Save(config);
            var loaded = service.Load();

            Assert.True(loaded.StartWithWindows);
            var group = Assert.Single(loaded.Groups);
            Assert.Equal("Editors", group.Name);
            Assert.NotNull(group.Hotkey);
            Assert.Equal("Ctrl+Shift+E", group.Hotkey!.ToString());
            Assert.Equal(new[] { "notepad", "code" }, group.ExecutableNames);
        }
        finally
        {
            TestPath.Clean(path);
        }
    }

    [Fact]
    public void Load_WhenFileCorrupt_ReturnsDefaults()
    {
        var path = TestPath.New();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ not valid json !!!");

            var service = new ConfigurationService(path, NullLogger.Instance);
            var config = service.Load();

            Assert.Equal(3, config.Groups.Count);
        }
        finally
        {
            TestPath.Clean(path);
        }
    }

    [Fact]
    public void Validate_NormalizesNamesIdsAndExecutables()
    {
        var service = new ConfigurationService(TestPath.New(), NullLogger.Instance);

        var config = new AppConfiguration
        {
            Groups =
            {
                new ApplicationGroup
                {
                    Id = "",
                    Name = "  GrOUP  ",
                    ExecutableNames = { "Chrome.EXE", "CHROME", "notepad.exe", "   " }
                },
                new ApplicationGroup
                {
                    Id = "",
                    Name = "group",
                    ExecutableNames = { "code" }
                }
            }
        };

        var result = service.Validate(config);

        Assert.Equal(2, result.Groups.Count);
        Assert.False(string.IsNullOrEmpty(result.Groups[0].Id));
        Assert.Equal("GrOUP", result.Groups[0].Name);
        Assert.Equal(new[] { "Chrome", "notepad" }, result.Groups[0].ExecutableNames);
        Assert.Equal("group (2)", result.Groups[1].Name);
    }

    [Fact]
    public void Validate_NullConfiguration_ReturnsDefaults()
    {
        var service = new ConfigurationService(TestPath.New(), NullLogger.Instance);
        var result = service.Validate(null);
        Assert.Equal(3, result.Groups.Count);
    }
}

internal static class TestPath
{
    public static string New() =>
        Path.Combine(Path.GetTempPath(), "WindowGroupsTests", Guid.NewGuid().ToString("N"), "config.json");

    public static void Clean(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir is not null && Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}