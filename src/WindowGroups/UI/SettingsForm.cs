using System;
using System.Drawing;
using System.Windows.Forms;
using WindowGroups.Core.Configuration;
using WindowGroups.Core.Models;
using WindowGroups.Core.Services;

namespace WindowGroups.UI;

/// <summary>Settings dialog for editing groups, shortcuts and startup behaviour.</summary>
public sealed class SettingsForm : Form
{
    private readonly Label _groupLabel;
    private readonly ListBox _groupListBox;
    private readonly Button _addGroupButton;
    private readonly Button _removeGroupButton;
    private readonly Button _restoreDefaultsButton;

    private readonly Label _nameLabel;
    private readonly TextBox _nameTextBox;
    private readonly Label _descriptionLabel;
    private readonly TextBox _descriptionTextBox;
    private readonly CheckBox _enabledCheckBox;
    private readonly Label _hotkeyLabel;
    private readonly TextBox _hotkeyTextBox;
    private readonly Button _setHotkeyButton;
    private readonly Button _clearHotkeyButton;
    private readonly Label _executablesLabel;
    private readonly ListBox _executablesListBox;
    private readonly TextBox _newExecutableTextBox;
    private readonly Button _addExecutableButton;
    private readonly Button _removeExecutableButton;

    private readonly CheckBox _startWithWindowsCheckBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    private readonly List<ApplicationGroup> _groups;
    private ApplicationGroup? _selectedGroup;
    private bool _updating;
    private bool _capturingHotkey;

    public SettingsForm(AppConfiguration configuration, bool startWithWindows)
    {
        _groups = configuration.Groups.Select(g => g.Clone()).ToList();

        Text = "Window Groups Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 500);
        KeyPreview = true;

        _groupLabel = NewLabel("Groups", 12, 12);
        _groupListBox = NewListBox(12, 32, 210, 340);
        _addGroupButton = NewButton("Add Group", 12, 382, 100, 28);
        _removeGroupButton = NewButton("Remove", 122, 382, 100, 28);
        _restoreDefaultsButton = NewButton("Restore Defaults", 12, 418, 210, 28);

        _nameLabel = NewLabel("Name", 240, 12);
        _nameTextBox = NewTextBox(240, 32, 300);
        _descriptionLabel = NewLabel("Description", 240, 62);
        _descriptionTextBox = NewTextBox(240, 82, 300);
        _enabledCheckBox = NewCheckBox("Enabled", 240, 116);
        _hotkeyLabel = NewLabel("Shortcut", 240, 142);
        _hotkeyTextBox = NewTextBox(240, 162, 160);
        _hotkeyTextBox.ReadOnly = true;
        _setHotkeyButton = NewButton("Set...", 408, 162, 64, 24);
        _clearHotkeyButton = NewButton("Clear", 478, 162, 64, 24);
        _executablesLabel = NewLabel("Executable names (without .exe)", 240, 196);
        _executablesListBox = NewListBox(240, 216, 300, 150);
        _newExecutableTextBox = NewTextBox(240, 374, 222);
        _addExecutableButton = NewButton("Add", 470, 374, 70, 24);
        _removeExecutableButton = NewButton("Remove Selected", 240, 408, 130, 26);

        _startWithWindowsCheckBox = NewCheckBox("Start with Windows", 12, 462);
        _startWithWindowsCheckBox.Checked = startWithWindows;
        _okButton = NewButton("OK", 600, 462, 72, 28);
        _cancelButton = NewButton("Cancel", 678, 462, 72, 28);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        WireEvents();

        RefreshGroups();
        if (_groups.Count > 0)
        {
            _groupListBox.SelectedIndex = 0;
        }
    }

    /// <summary>The edited configuration when the dialog was accepted; null otherwise.</summary>
    public AppConfiguration? UpdatedConfiguration { get; private set; }

    public bool StartWithWindows => _startWithWindowsCheckBox.Checked;

    private void WireEvents()
    {
        _groupListBox.SelectedIndexChanged += (_, _) => OnGroupSelectionChanged();
        _addGroupButton.Click += (_, _) => AddGroup();
        _removeGroupButton.Click += (_, _) => RemoveGroup();
        _restoreDefaultsButton.Click += (_, _) => RestoreDefaults();

        _setHotkeyButton.Click += (_, _) => BeginHotkeyCapture();
        _clearHotkeyButton.Click += (_, _) => ClearHotkey();
        _addExecutableButton.Click += (_, _) => AddExecutable();
        _removeExecutableButton.Click += (_, _) => RemoveExecutable();
        _newExecutableTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AddExecutable();
            }
        };

        _okButton.Click += (_, _) => OkButton_Click();
        _cancelButton.Click += (_, _) => Close();
    }

    private Label NewLabel(string text, int x, int y)
    {
        var control = new Label { Text = text, Location = new Point(x, y), AutoSize = true };
        Controls.Add(control);
        return control;
    }

    private TextBox NewTextBox(int x, int y, int width)
    {
        var control = new TextBox { Location = new Point(x, y), Size = new Size(width, 23) };
        Controls.Add(control);
        return control;
    }

    private CheckBox NewCheckBox(string text, int x, int y)
    {
        var control = new CheckBox { Text = text, Location = new Point(x, y), AutoSize = true };
        Controls.Add(control);
        return control;
    }

    private Button NewButton(string text, int x, int y, int width, int height)
    {
        var control = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height)
        };
        Controls.Add(control);
        return control;
    }

    private ListBox NewListBox(int x, int y, int width, int height)
    {
        var control = new ListBox
        {
            Location = new Point(x, y),
            Size = new Size(width, height)
        };
        Controls.Add(control);
        return control;
    }

    private void OnGroupSelectionChanged()
    {
        if (_updating) return;
        CommitFields();

        var index = _groupListBox.SelectedIndex;
        _selectedGroup = index >= 0 && index < _groups.Count ? _groups[index] : null;
        LoadFields();
    }

    private void LoadFields()
    {
        if (_selectedGroup is null)
        {
            _nameTextBox.Text = string.Empty;
            _descriptionTextBox.Text = string.Empty;
            _enabledCheckBox.Checked = true;
            _hotkeyTextBox.Text = string.Empty;
            _executablesListBox.Items.Clear();
            SetEditorEnabled(false);
            return;
        }

        SetEditorEnabled(true);
        _nameTextBox.Text = _selectedGroup.Name;
        _descriptionTextBox.Text = _selectedGroup.Description ?? string.Empty;
        _enabledCheckBox.Checked = _selectedGroup.Enabled;
        UpdateHotkeyDisplay();
        RefreshExecutables();
    }

    private void CommitFields()
    {
        if (_selectedGroup is null) return;
        _selectedGroup.Name = _nameTextBox.Text.Trim();
        _selectedGroup.Description = _descriptionTextBox.Text.Trim();
        _selectedGroup.Enabled = _enabledCheckBox.Checked;
    }

    private void SetEditorEnabled(bool enabled)
    {
        _nameTextBox.Enabled = enabled;
        _descriptionTextBox.Enabled = enabled;
        _enabledCheckBox.Enabled = enabled;
        _hotkeyTextBox.Enabled = enabled;
        _setHotkeyButton.Enabled = enabled;
        _clearHotkeyButton.Enabled = enabled;
        _executablesListBox.Enabled = enabled;
        _newExecutableTextBox.Enabled = enabled;
        _addExecutableButton.Enabled = enabled;
        _removeExecutableButton.Enabled = enabled;
    }

    private void UpdateHotkeyDisplay() =>
        _hotkeyTextBox.Text = _selectedGroup?.Hotkey?.ToString() ?? string.Empty;

    private void RefreshExecutables()
    {
        _executablesListBox.BeginUpdate();
        try
        {
            _executablesListBox.Items.Clear();
            if (_selectedGroup is not null)
            {
                foreach (var name in _selectedGroup.ExecutableNames)
                {
                    _executablesListBox.Items.Add(name);
                }
            }
        }
        finally
        {
            _executablesListBox.EndUpdate();
        }
    }

    private void RefreshGroups()
    {
        _updating = true;
        try
        {
            var selectedIndex = _groupListBox.SelectedIndex;
            _groupListBox.Items.Clear();
            foreach (var group in _groups)
            {
                _groupListBox.Items.Add(group.Name);
            }

            if (selectedIndex >= 0 && selectedIndex < _groups.Count)
            {
                _groupListBox.SelectedIndex = selectedIndex;
            }
        }
        finally
        {
            _updating = false;
        }
    }

    private void AddGroup()
    {
        CommitFields();
        _groups.Add(new ApplicationGroup { Name = "New Group", Enabled = true });
        RefreshGroups();
        _groupListBox.SelectedIndex = _groups.Count - 1;
    }

    private void RemoveGroup()
    {
        var index = _groupListBox.SelectedIndex;
        if (index < 0) return;

        _selectedGroup = null;
        _groups.RemoveAt(index);
        RefreshGroups();

        var newIndex = Math.Min(index, _groups.Count - 1);
        if (newIndex >= 0)
        {
            _groupListBox.SelectedIndex = newIndex;
        }
        else
        {
            LoadFields();
        }
    }

    private void RestoreDefaults()
    {
        var result = MessageBox.Show(
            "Replace all groups with the default groups?",
            "Restore Defaults",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        if (result != DialogResult.OK) return;

        _groups.Clear();
        _groups.AddRange(DefaultGroups.CreateDefaultGroups().Select(g => g.Clone()));
        _selectedGroup = null;
        RefreshGroups();
        _groupListBox.SelectedIndex = 0;
    }

    private void AddExecutable()
    {
        if (_selectedGroup is null) return;

        var name = ExecutableMatcher.NormalizeExecutableName(_newExecutableTextBox.Text);
        if (name.Length == 0) return;

        if (_selectedGroup.ExecutableNames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            _newExecutableTextBox.Clear();
            return;
        }

        _selectedGroup.ExecutableNames.Add(name);
        _newExecutableTextBox.Clear();
        RefreshExecutables();
    }

    private void RemoveExecutable()
    {
        if (_selectedGroup is null) return;

        var index = _executablesListBox.SelectedIndex;
        if (index < 0 || index >= _selectedGroup.ExecutableNames.Count) return;

        _selectedGroup.ExecutableNames.RemoveAt(index);
        RefreshExecutables();
    }

    private void BeginHotkeyCapture()
    {
        if (_selectedGroup is null) return;
        _capturingHotkey = true;
        _setHotkeyButton.Text = "Press keys...";
        _hotkeyTextBox.Text = string.Empty;
    }

    private void ClearHotkey()
    {
        if (_selectedGroup is null) return;
        _selectedGroup.Hotkey = null;
        UpdateHotkeyDisplay();
    }

    private void CancelHotkeyCapture()
    {
        _capturingHotkey = false;
        _setHotkeyButton.Text = "Set...";
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_capturingHotkey)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.Escape)
            {
                CancelHotkeyCapture();
            }
            else
            {
                var hotkey = HotkeyMapper.FromKeyData(e.KeyData);
                if (hotkey is not null && _selectedGroup is not null)
                {
                    _selectedGroup.Hotkey = hotkey;
                    CancelHotkeyCapture();
                    UpdateHotkeyDisplay();
                }
            }

            return;
        }

        base.OnKeyDown(e);
    }

    private void OkButton_Click()
    {
        if (_capturingHotkey) CancelHotkeyCapture();
        CommitFields();

        var errors = ValidateGroups();
        if (errors.Count > 0)
        {
            MessageBox.Show(
                "Please fix the following issues:\n\n" + string.Join("\n", errors),
                "Window Groups",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        UpdatedConfiguration = new AppConfiguration
        {
            Version = 1,
            StartWithWindows = _startWithWindowsCheckBox.Checked,
            Groups = _groups.Select(g => g.Clone()).ToList()
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private List<string> ValidateGroups()
    {
        var errors = new List<string>();

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in _groups)
        {
            if (string.IsNullOrWhiteSpace(group.Name))
            {
                errors.Add("Every group must have a name.");
            }
            else if (!names.Add(group.Name))
            {
                errors.Add($"Duplicate group name: '{group.Name}'.");
            }

            if (group.ExecutableNames.Count == 0)
            {
                errors.Add($"Group '{group.Name}' has no executable names.");
            }
        }

        var shortcuts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in _groups.Where(g => g.Enabled && g.Hotkey is not null))
        {
            var text = group.Hotkey!.ToString();
            if (!shortcuts.Add(text))
            {
                errors.Add($"Duplicate shortcut '{text}' used by more than one enabled group.");
            }
        }

        return errors;
    }
}