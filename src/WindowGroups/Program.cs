using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WindowGroups.Core.Configuration;
using WindowGroups.Core.Interop;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;
using WindowGroups.Core.Services;
using WindowGroups.UI;

namespace WindowGroups;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\WindowGroups.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var logger = new FileLogger(
            Path.Combine(ConfigurationService.GetDefaultConfigDirectory(), "windowgroups.log"));

        // A launch may carry a command (e.g. from a taskbar jump-list task).
        var requestedCommand = CommandLineParser.Parse(args);

        // Create the cross-process command channel up front, and hold a
        // single-instance mutex for the lifetime of the process.
        using var channel = new CommandChannel();
        using var mutex = new Mutex(initiallyOwned: false, name: SingleInstanceMutexName, out _);

        bool isFirstInstance;
        try
        {
            isFirstInstance = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            // A previous instance crashed without releasing the mutex; take it over.
            isFirstInstance = true;
        }

        if (!isFirstInstance)
        {
            // A plain second launch surfaces the running instance's settings; a
            // jump-list launch forwards its command.
            channel.Signal(requestedCommand ?? new CommandRequest(CommandKind.ShowSettings, null));
            logger.Information("Signalled the running instance; this instance will exit.");
            return;
        }

        TaskbarService.Initialize();

        try
        {
            var configurationService = new ConfigurationService(
                ConfigurationService.GetDefaultConfigFilePath(), logger);
            var configuration = configurationService.Load();

            IWindowApi windowApi = new WindowApi();
            var processNameResolver = new SystemProcessNameResolver();
            var enumerator = new ProcessWindowEnumerator(windowApi, processNameResolver, Environment.ProcessId, logger);
            var windowManager = new WindowManager(windowApi, enumerator, logger);

            IHotkeyApi hotkeyApi = new HotkeyApi();
            var startupService = new RegistryStartupService(logger);

            using var trayContext = new TrayApplicationContext(
                configuration, configurationService, windowManager, hotkeyApi, startupService, logger);

            channel.StartListening(trayContext.PostCommand);

            // A launch that already carried a command (jump list while the app was
            // closed) applies it once the message loop starts.
            if (requestedCommand is not null)
            {
                trayContext.PostCommand(requestedCommand);
            }

            Application.Run(trayContext);
        }
        catch (Exception ex)
        {
            logger.Error("Fatal error during startup.", ex);
            MessageBox.Show(
                $"Window Groups failed to start:\n\n{ex.Message}",
                "Window Groups",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}