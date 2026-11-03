namespace WindowGroups.Core.Logging;

/// <summary>
/// Appends log lines to a plain-text file, rotating to a ".old" backup once the
/// file grows past a size limit so the log never grows without bound. Failures
/// are swallowed so logging can never take the application down.
/// </summary>
public sealed class FileLogger : ILogger
{
    public const long DefaultMaxLogBytes = 1024 * 1024; // 1 MB

    private readonly string _filePath;
    private readonly long _maxBytes;
    private readonly object _sync = new();
    private readonly bool _available;

    public FileLogger(string filePath)
        : this(filePath, DefaultMaxLogBytes)
    {
    }

    public FileLogger(string filePath, long maxBytes)
    {
        _filePath = filePath;
        _maxBytes = maxBytes;

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
                RotateIfNeeded();
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

    private void RotateIfNeeded()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            if (new FileInfo(_filePath).Length < _maxBytes)
            {
                return;
            }

            File.Move(_filePath, _filePath + ".old", overwrite: true);
        }
        catch
        {
            // Rotation failure is non-fatal; keep appending.
        }
    }
}