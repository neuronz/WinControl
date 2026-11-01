using System.Runtime.InteropServices;
using System.Windows.Shell;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;

namespace WindowGroups.UI;

/// <summary>
/// Sets the process AppUserModelID and maintains the taskbar jump list so the
/// group actions are available from the pinned taskbar icon.
/// </summary>
internal static class TaskbarService
{
    public const string AppUserModelId = "WindowGroups.App";

    [DllImport("shell32.dll")]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

    public static void Initialize()
    {
        try
        {
            _ = SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        }
        catch
        {
            // Non-fatal; the jump list may simply not attach to the taskbar icon.
        }
    }

    public static void UpdateJumpList(AppConfiguration configuration, ILogger logger)
    {
        try
        {
            var launchPath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(launchPath))
            {
                return;
            }

            var workingDirectory = System.IO.Path.GetDirectoryName(launchPath) ?? string.Empty;

            var jumpList = new JumpList
            {
                ShowRecentCategory = false,
                ShowFrequentCategory = false
            };

            jumpList.JumpItems.Add(BuildTask("Minimize All", "--minimize-all", launchPath, workingDirectory));
            jumpList.JumpItems.Add(BuildTask("Restore All", "--restore-all", launchPath, workingDirectory));

            foreach (var group in configuration.Groups.Where(g => g.Enabled))
            {
                var label = GroupDisplay.WithShortcut(group.Name, group.Hotkey);
                jumpList.JumpItems.Add(BuildTask($"Minimize {label}", "--minimize " + group.Id, launchPath, workingDirectory));
                jumpList.JumpItems.Add(BuildTask($"Restore {label}", "--restore " + group.Id, launchPath, workingDirectory));
            }

            jumpList.JumpItems.Add(BuildTask("Settings", "--settings", launchPath, workingDirectory));
            jumpList.JumpItems.Add(BuildTask("Exit", "--exit", launchPath, workingDirectory));

            jumpList.Apply();
        }
        catch (Exception ex)
        {
            logger.Warning("Failed to update the taskbar jump list: " + ex.Message);
        }
    }

    private static JumpTask BuildTask(string title, string arguments, string launchPath, string workingDirectory) => new()
    {
        Title = title,
        Arguments = arguments,
        ApplicationPath = launchPath,
        WorkingDirectory = workingDirectory
    };
}