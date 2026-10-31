namespace WindowGroups.Core.Logging;

/// <summary>
/// Appends log lines to a plain-text file. Failures are swallowed so that
/// logging can never take the application down.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly object _sync = new();
    private readonly bool _available;

    public FileLogger(string filePath)
    {
        _filePath = filePath;
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _available = true;
        }
        catch
        {
            _available = false;
        }
    }

    public void Log(LogLevel level, string message)
    {
        if (!_available) return;

        try
        {
            lock (_sync)
            {
                File.AppendAllText(
                    _filePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Best effort only.
        }
    }
}