using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowGroups.UI;

/// <summary>Simple "About" dialog showing name, version, link and licence.</summary>
public sealed class AboutForm : Form
{
    public const string RepositoryUrl = "https://github.com/neuronz/WinControl";

    private readonly Icon _appIcon;

    public AboutForm()
    {
        Text = "About Window Groups";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(430, 230);

        _appIcon = LoadIcon();
        Icon = _appIcon;

        var title = new Label
        {
            Text = $"Window Groups  v{AppVersion.Current}",
            Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 16)
        };

        var description = new Label
        {
            Text = "A lightweight Windows 11 tray utility that minimises and restores groups of application windows with one click or a hotkey.",
            AutoSize = false,
            Size = new Size(390, 40),
            Location = new Point(16, 52)
        };

        var link = new LinkLabel
        {
            Text = RepositoryUrl,
            AutoSize = true,
            Location = new Point(16, 108)
        };
        link.LinkClicked += (_, _) => OpenRepository();

        var copyright = new Label
        {
            Text = "Copyright © 2026 Bruce Bayley  ·  MIT License",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(16, 138)
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(90, 28),
            Location = new Point(ClientSize.Width - 106, 188)
        };

        Controls.AddRange(new Control[] { title, description, link, copyright, okButton });

        AcceptButton = okButton;
        CancelButton = okButton;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        _appIcon.Dispose();
    }

    private static void OpenRepository()
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });
        }
        catch
        {
            // The link is informational; ignore failures.
        }
    }

    private static Icon LoadIcon()
    {
        using var stream = typeof(TrayApplicationContext).Assembly
            .GetManifestResourceStream("WindowGroups.app_icon.ico");

        return stream is null ? SystemIcons.Application : new Icon(stream);
    }
}