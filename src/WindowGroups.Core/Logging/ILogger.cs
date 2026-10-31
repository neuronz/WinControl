namespace WindowGroups.Core.Logging;

/// <summary>
/// Minimal logging abstraction so the core library does not depend on a third
/// party logging framework.
/// </summary>
public interface ILogger
{
    void Log(LogLevel level, string message);
}