using Microsoft.Win32;
using System.Windows.Forms;

namespace SdxcCopy;

/// <summary>
/// Inställningsfönstret: listan över kameror (grundkatalog och mappmönster)
/// samt autostart med Windows.
/// </summary>
public sealed class SettingsForm : Form
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "SDXC-copy";

    private readonly AppConfig _config;
    private readonly ListView _cameraList = new()
    {
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = false,
        Dock = DockStyle.Fill,
    };

    public SettingsForm(AppConfig config)
    {
        _config = config;

        Text = "Inställningar — SDXC-copy";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new System.Drawing.Size(640, 320);
        MinimumSize = new System.Drawing.Size(520, 260);

        _cameraList.Columns.Add("Kamera", 180);
        _cameraList.Columns.Add("Grundkatalog", 260);
        _cameraList.Columns.Add("Mappmönster", 170);
        _cameraList.DoubleClick += (_, _) => EditSelected();

        var editButton = new Button { Text = "Ändra…", Width = 100 };
        editButton.Click += (_, _) => EditSelected();
        var removeButton = new Button { Text = "Ta bort", Width = 100 };
        removeButton.Click += (_, _) => RemoveSelected();
        var closeButton = new Button { Text = "Stäng", Width = 100, DialogResult = DialogResult.OK };
        CancelButton = closeButton;

        var autostartBox = new CheckBox
        {
            Text = "Starta SDXC-copy automatiskt med Windows",
            AutoSize = true,
            Checked = IsAutostartEnabled(),
        };
        autostartBox.CheckedChanged += (_, _) => SetAutostart(autostartBox.Checked);

        var sidePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Right,
            Width = 116,
            Padding = new Padding(8, 0, 0, 0),
        };
        sidePanel.Controls.Add(editButton);
        sidePanel.Controls.Add(removeButton);

        var bottomPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(8),
        };
        bottomPanel.Controls.Add(autostartBox);
        closeButton.Anchor = AnchorStyles.Right;

        var closePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(8),
        };
        closePanel.Controls.Add(closeButton);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        content.Controls.Add(_cameraList);
        content.Controls.Add(sidePanel);

        Controls.Add(content);
        Controls.Add(bottomPanel);
        Controls.Add(closePanel);

        RefreshCameraList();
    }

    private void RefreshCameraList()
    {
        _cameraList.Items.Clear();
        foreach (var camera in _config.Cameras)
        {
            var item = new ListViewItem(new[] { camera.DisplayName, camera.BaseDirectory, camera.FolderPattern })
            {
                Tag = camera,
            };
            _cameraList.Items.Add(item);
        }
    }

    private CameraConfig? SelectedCamera =>
        _cameraList.SelectedItems.Count > 0 ? (CameraConfig)_cameraList.SelectedItems[0].Tag! : null;

    private void EditSelected()
    {
        var camera = SelectedCamera;
        if (camera is null)
            return;

        using var form = new CameraForm(camera, isNew: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            ConfigStore.Save(_config);
            RefreshCameraList();
        }
    }

    private void RemoveSelected()
    {
        var camera = SelectedCamera;
        if (camera is null)
            return;

        var answer = MessageBox.Show(
            this,
            $"Ta bort \"{camera.DisplayName}\"? Inga bilder raderas — bara kopplingen till grundkatalogen glöms.",
            "SDXC-copy",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer == DialogResult.Yes)
        {
            _config.Cameras.Remove(camera);
            ConfigStore.Save(_config);
            RefreshCameraList();
        }
    }

    private static bool IsAutostartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) is not null;
    }

    private static void SetAutostart(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
            key.SetValue(RunValueName, $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
    }
}
