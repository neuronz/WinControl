namespace WindowGroups.Core.Services;

/// <summary>Pure matching logic between a process name and configured executable names.</summary>
public static class ExecutableMatcher
{
    public static bool Matches(string? processName, IReadOnlyCollection<string> executableNames)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;

        var normalizedProcessName = NormalizeExecutableName(processName);
        if (normalizedProcessName.Length == 0) return false;

        foreach (var name in executableNames)
        {
            var normalized = NormalizeExecutableName(name);
            if (normalized.Length == 0) continue;

            if (string.Equals(normalizedProcessName, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Trims whitespace and strips a trailing ".exe" (case-insensitive).</summary>
    public static string NormalizeExecutableName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var trimmed = name.Trim();
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^4].Trim();
        }

        return trimmed;
    }
}