using System.Windows.Forms;
using WindowGroups.Core.Services;

namespace WindowGroups.UI;

/// <summary>
/// A hidden window that owns the message loop for global hotkeys and forwards
/// WM_HOTKEY messages to the <see cref="HotkeyManager"/>.
/// </summary>
internal sealed class HotkeyMessageWindow : Form
{
    private const int VmHotkey = 0x0312;

    private readonly HotkeyManager _hotkeyManager;

    public HotkeyMessageWindow(HotkeyManager hotkeyManager)
    {
        _hotkeyManager = hotkeyManager;

        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowIcon = false;
        MinimizeBox = false;
        MaximizeBox = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == VmHotkey)
        {
            _hotkeyManager.TryHandleHotkey((int)m.WParam);
            return;
        }

        base.WndProc(ref m);
    }
}