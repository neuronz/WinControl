namespace WindowGroups.Core.Logging;

/// <summary>A logger that discards all messages. Useful for tests.</summary>
public sealed class NullLogger : ILogger
{
    public static readonly NullLogger Instance = new();

    private NullLogger()
    {
    }

    public void Log(LogLevel level, string message)
    {
    }
}