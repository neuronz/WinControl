using WindowGroups.Core.Models;

namespace WindowGroups.Core.Services;

/// <summary>Parses command-line arguments into a <see cref="CommandRequest"/>.</summary>
public static class CommandLineParser
{
    public static CommandRequest? Parse(string[]? args)
    {
        if (args is null || args.Length == 0) return null;

        var command = (args[0] ?? string.Empty).Trim().ToLowerInvariant();
        switch (command)
        {
            case "--settings":
                return new CommandRequest(CommandKind.ShowSettings, null);
            case "--exit":
                return new CommandRequest(CommandKind.Exit, null);
            case "--minimize":
                return Build(CommandKind.MinimizeGroup, args);
            case "--restore":
                return Build(CommandKind.RestoreGroup, args);
            case "--minimize-all":
                return new CommandRequest(CommandKind.MinimizeAll, null);
            case "--restore-all":
                return new CommandRequest(CommandKind.RestoreAll, null);
            default:
                return null;
        }
    }

    private static CommandRequest? Build(CommandKind kind, string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
        {
            return null;
        }

        return new CommandRequest(kind, args[1].Trim());
    }
}