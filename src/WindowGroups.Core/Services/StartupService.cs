using Microsoft.Win32;
using WindowGroups.Core.Logging;

namespace WindowGroups.Core.Services;

/// <summary>Manages the "start with Windows" behaviour.</summary>
public interface IStartupService
{
    bool IsEnabled();

    void SetEnabled(bool enabled);
}

/// <summary>
/// Implements automatic startup via the current user's Run registry key, which
/// requires no administrator privileges.
/// </summary>
public sealed class RegistryStartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowGroups";
    private readonly ILogger _logger;

    public RegistryStartupService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string value && value.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to read the startup setting: {ex.Message}");
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath)
                ?? throw new InvalidOperationException("Failed to open the current user Run registry key.");

            if (enabled)
            {
                var executablePath = Environment.ProcessPath
                    ?? throw new InvalidOperationException("Cannot determine the application executable path.");
                key.SetValue(ValueName, $"\"{executablePath}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to update the startup setting.", ex);
            throw;
        }
    }
}