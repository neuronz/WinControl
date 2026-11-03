using System.Reflection;

namespace WindowGroups;

/// <summary>Exposes the application version for display.</summary>
internal static class AppVersion
{
    public static string Current { get; } = Read();

    private static string Read()
    {
        try
        {
            var version = typeof(Program).Assembly.GetName().Version;
            return version is null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            return "1.0.0";
        }
    }
}