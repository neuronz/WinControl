using System.Drawing;
using System.Windows.Forms;
using WindowGroups.Core.Configuration;
using WindowGroups.Core.Interop;
using WindowGroups.Core.Logging;
using WindowGroups.Core.Models;
using WindowGroups.Core.Services;

namespace WindowGroups.UI;

/// <summary>
/// Hosts the system-tray icon and its context menu, and wires up global
/// hotkeys. There is no main window.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ConfigurationService _configurationService;
    private readonly WindowManager _windowManager;
    private readonly IStartupService _startupService;
    private readonly ILogger _logger;
    private readonly HotkeyManager _hotkeyManager;
    private readonly HotkeyMessageWindow _messageWindow;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;

    private AppConfiguration _configuration;

    public TrayApplicationContext(
        AppConfiguration configuration,
        ConfigurationService configurationService,
        WindowManager windowManager,
        IHotkeyApi hotkeyApi,
        IStartupService startupService,
        ILogger logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _hotkeyManager = new HotkeyManager(hotkeyApi ?? throw new ArgumentNullException(nameof(hotkeyApi)), logger);

        _icon = LoadIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = $"Window Groups v{AppVersion.Current}",
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _messageWindow = new HotkeyMessageWindow(_hotkeyManager);
        _ = _messageWindow.Handle; // Force creation of the underlying window.

        RebuildMenu();
        RegisterHotkeys();
        TaskbarService.UpdateJumpList(_configuration, _logger);
    }

    protected override void ExitThreadCore()
    {
        _hotkeyManager.Dispose();
        _messageWindow.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
        base.ExitThreadCore();
    }

    private void RebuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            BuildGroupSubmenu("Minimize Groups", minimize: true),
            BuildGroupSubmenu("Restore Groups", minimize: false),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Settings", null, (_, _) => ShowSettings()),
            new ToolStripMenuItem("About", null, (_, _) => ShowAbout()),
            new ToolStripMenuItem("Exit", null, (_, _) => ExitThread())
        });

        var previous = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = menu;
        previous?.Dispose();
    }

    private void ShowAbout()
    {
        using var form = new AboutForm();
        form.ShowDialog();
    }

    private ToolStripMenuItem BuildGroupSubmenu(string title, bool minimize)
    {
        var submenu = new ToolStripMenuItem(title);

        submenu.DropDownItems.Add(new ToolStripMenuItem(
            minimize ? "Minimize All" : "Restore All",
            null,
            (_, _) => RunAllAction(minimize)));
        submenu.DropDownItems.Add(new ToolStripSeparator());

        foreach (var group in _configuration.Groups)
        {
            var captured = group;
            var item = new ToolStripMenuItem(GroupDisplay.WithShortcut(group.Name, group.Hotkey))
            {
                Enabled = group.Enabled,
                ToolTipText = BuildGroupTooltip(group)
            };
            item.Click += (_, _) => RunGroupAction(captured, minimize);
            submenu.DropDownItems.Add(item);
        }

        return submenu;
    }

    private void RunAllAction(bool minimize)
    {
        try
        {
            if (minimize)
            {
                _windowManager.MinimizeAll(_configuration.Groups);
            }
            else
            {
                _windowManager.RestoreAll(_configuration.Groups);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(minimize ? "Minimize All failed." : "Restore All failed.", ex);
        }
    }

    private static string BuildGroupTooltip(ApplicationGroup group)
    {
        return string.IsNullOrWhiteSpace(group.Description) ? string.Empty : group.Description;
    }

    private void RunGroupAction(ApplicationGroup group, bool minimize)
    {
        try
        {
            if (minimize)
            {
                _windowManager.MinimizeGroup(group);
            }
            else
            {
                _windowManager.RestoreGroup(group);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Group action failed for '{group.Name}'.", ex);
        }
    }

    private void RunToggleAction(ApplicationGroup group)
    {
        try
        {
            _windowManager.ToggleGroup(group);
        }
        catch (Exception ex)
        {
            _logger.Error($"Toggle action failed for '{group.Name}'.", ex);
        }
    }

    private void RegisterHotkeys()
    {
        _hotkeyManager.UnregisterAll();

        foreach (var group in _configuration.Groups)
        {
            if (!group.Enabled || group.Hotkey is null)
            {
                continue;
            }

            var captured = group;
            try
            {
                _hotkeyManager.Register(
                    _messageWindow.Handle,
                    captured.Hotkey,
                    () => RunToggleAction(captured));
            }
            catch (HotkeyConflictException ex)
            {
                _logger.Warning(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to register hotkey for group '{group.Name}'.", ex);
            }
        }
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_configuration, _startupService.IsEnabled());
        if (form.ShowDialog() != DialogResult.OK || form.UpdatedConfiguration is null)
        {
            return;
        }

        _configuration = form.UpdatedConfiguration;

        try
        {
            _configurationService.Save(_configuration);
            _startupService.SetEnabled(form.StartWithWindows);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings.", ex);
            MessageBox.Show(
                "Failed to save settings:\n\n" + ex.Message,
                "Window Groups",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        RebuildMenu();
        RegisterHotkeys();
        TaskbarService.UpdateJumpList(_configuration, _logger);
    }

    /// <summary>
    /// Queues a command (for example, from a second instance or jump-list task)
    /// onto the UI thread. Safe to call from any thread.
    /// </summary>
    public void PostCommand(CommandRequest command)
    {
        try
        {
            if (_messageWindow.IsHandleCreated)
            {
                _messageWindow.BeginInvoke(() => ExecuteCommand(command));
            }
        }
        catch
        {
            // Ignore commands that race application shutdown.
        }
    }

    private void ExecuteCommand(CommandRequest command)
    {
        switch (command.Kind)
        {
            case CommandKind.ShowSettings:
                ShowSettings();
                break;

            case CommandKind.Exit:
                ExitThread();
                break;

            case CommandKind.MinimizeAll:
                RunAllAction(minimize: true);
                break;

            case CommandKind.RestoreAll:
                RunAllAction(minimize: false);
                break;

            case CommandKind.MinimizeGroup:
            case CommandKind.RestoreGroup:
                var group = FindGroup(command.GroupId);
                if (group is not null)
                {
                    RunGroupAction(group, command.Kind == CommandKind.MinimizeGroup);
                }

                break;
        }
    }

    private ApplicationGroup? FindGroup(string? idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName))
        {
            return null;
        }

        return _configuration.Groups.FirstOrDefault(g =>
            string.Equals(g.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(g.Name, idOrName, StringComparison.OrdinalIgnoreCase));
    }

    private static Icon LoadIcon()
    {
        using var stream = typeof(TrayApplicationContext).Assembly
            .GetManifestResourceStream("WindowGroups.app_icon.ico");

        return stream is null ? SystemIcons.Application : new Icon(stream);
    }
}