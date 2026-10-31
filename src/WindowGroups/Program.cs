using System;
using System.Windows.Forms;
using WindowGroups.Core.Configuration;
using WindowGroups.Core.Interop;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Services;
using WindowGroups.UI;

namespace WindowGroups;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var logger = new FileLogger(
            Path.Combine(ConfigurationService.GetDefaultConfigDirectory(), "windowgroups.log"));

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