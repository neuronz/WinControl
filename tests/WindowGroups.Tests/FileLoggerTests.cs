using WindowGroups.Core.Logging;
using Xunit;

namespace WindowGroups.Tests;

public sealed class FileLoggerTests
{
    [Fact]
    public void Log_RotatesFileOnceItExceedsMaximumSize()
    {
        var directory = Path.Combine(Path.GetTempPath(), "WindowGroupsTests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "windowgroups.log");

        try
        {
            var logger = new FileLogger(path, maxBytes: 200);

            for (var i = 0; i < 20; i++)
            {
                logger.Log(LogLevel.Information, new string('x', 50));
            }

            // Without rotation this would be ~1800+ bytes; rotation must cap it.
            Assert.True(new FileInfo(path).Length < 500, "Log grew without bound.");
            Assert.True(File.Exists(path + ".old"), "Expected a '.old' rotated log file.");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}