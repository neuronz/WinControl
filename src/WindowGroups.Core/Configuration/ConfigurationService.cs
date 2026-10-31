using System.Text.Json;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;
using WindowGroups.Core.Services;

namespace WindowGroups.Core.Configuration;

/// <summary>
/// Loads and saves the application configuration as JSON. Loading is resilient:
/// a missing or corrupt file yields the default configuration instead of
/// throwing.
/// </summary>
public sealed class ConfigurationService
{
    public const string DirectoryName = "WindowGroups";
    public const string ConfigFileName = "config.json";

    private readonly string _configFilePath;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService(string configFilePath, ILogger logger)
    {
        _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new HotkeyJsonConverter() }
        };
    }

    public static string GetDefaultConfigDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DirectoryName);

    public static string GetDefaultConfigFilePath() =>
        Path.Combine(GetDefaultConfigDirectory(), ConfigFileName);

    public AppConfiguration Load()
    {
        if (!File.Exists(_configFilePath))
        {
            _logger.Information($"Configuration file '{_configFilePath}' not found; using defaults.");
            return DefaultGroups.CreateDefaultConfiguration();
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
            return Validate(config);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load configuration from '{_configFilePath}'; using defaults.", ex);
            TryBackupCorruptFile();
            return DefaultGroups.CreateDefaultConfiguration();
        }
    }

    public void Save(AppConfiguration config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        var validated = Validate(config);

        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(validated, _jsonOptions);
        File.WriteAllText(_configFilePath, json);
    }

    /// <summary>
    /// Normalises and repairs a configuration: trims names, assigns missing
    /// group ids, deduplicates executable names (case-insensitively) and makes
    /// group names unique.
    /// </summary>
    public AppConfiguration Validate(AppConfiguration? config)
    {
        if (config is null)
        {
            return DefaultGroups.CreateDefaultConfiguration();
        }

        var result = new AppConfiguration
        {
            Version = config.Version,
            StartWithWindows = config.StartWithWindows
        };

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in config.Groups ?? new List<ApplicationGroup>())
        {
            if (group is null) continue;

            var id = string.IsNullOrWhiteSpace(group.Id) ? Guid.NewGuid().ToString("D") : group.Id.Trim();
            if (!seenIds.Add(id))
            {
                id = Guid.NewGuid().ToString("D");
                seenIds.Add(id);
            }

            var baseName = string.IsNullOrWhiteSpace(group.Name) ? "Group" : group.Name.Trim();
            var uniqueName = baseName;
            var suffix = 2;
            while (!seenNames.Add(uniqueName))
            {
                uniqueName = $"{baseName} ({suffix})";
                suffix++;
            }

            var executables = new List<string>();
            var seenExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in group.ExecutableNames ?? new List<string>())
            {
                var normalized = ExecutableMatcher.NormalizeExecutableName(name);
                if (normalized.Length == 0) continue;
                if (seenExecutables.Add(normalized))
                {
                    executables.Add(normalized);
                }
            }

            result.Groups.Add(new ApplicationGroup
            {
                Id = id,
                Name = uniqueName,
                Description = string.IsNullOrWhiteSpace(group.Description) ? null : group.Description.Trim(),
                Enabled = group.Enabled,
                Hotkey = group.Hotkey,
                ExecutableNames = executables
            });
        }

        return result;
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var backupPath = _configFilePath + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}";
                File.Copy(_configFilePath, backupPath, overwrite: true);
                _logger.Information($"Backed up corrupt configuration to '{backupPath}'.");
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Could not back up the corrupt configuration file: {ex.Message}");
        }
    }
}