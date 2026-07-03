using Microsoft.Win32;
using System.Drawing;
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
        Icon = AppIcon.Get();
        // Skalning efter textstorlek så att fönstret fungerar på skärmar
        // med 125/150 % DPI-skalning.
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(780, 440);
        MinimumSize = new Size(640, 400);

        _cameraList.Columns.Add("Kamera", 190);
        _cameraList.Columns.Add("Grundkatalog", 330);
        _cameraList.Columns.Add("Mappmönster", 210);
        _cameraList.DoubleClick += (_, _) => EditSelected();

        Button MakeButton(string text)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(120, 34),
                Margin = new Padding(0, 0, 0, 8),
            };
            return button;
        }

        var editButton = MakeButton("Ändra…");
        editButton.Click += (_, _) => EditSelected();
        var removeButton = MakeButton("Ta bort");
        removeButton.Click += (_, _) => RemoveSelected();
        var closeButton = MakeButton("Stäng");
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Anchor = AnchorStyles.Right;
        CancelButton = closeButton;

        var autostartBox = new CheckBox
        {
            Text = "Starta SDXC-copy automatiskt med Windows",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Checked = IsAutostartEnabled(),
        };
        autostartBox.CheckedChanged += (_, _) => SetAutostart(autostartBox.Checked);

        var sideButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(12, 0, 0, 0),
        };
        sideButtons.Controls.Add(editButton);
        sideButtons.Controls.Add(removeButton);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(16),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(_cameraList, 0, 0);
        layout.Controls.Add(sideButtons, 1, 0);

        var bottomRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0),
        };
        bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomRow.Controls.Add(autostartBox, 0, 0);
        bottomRow.Controls.Add(closeButton, 1, 0);

        layout.Controls.Add(bottomRow, 0, 1);
        layout.SetColumnSpan(bottomRow, 2);

        Controls.Add(layout);

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
        {
            MessageBox.Show(this, "Markera en kamera i listan först.", "SDXC-copy");
            return;
        }

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
        {
            MessageBox.Show(this, "Markera en kamera i listan först.", "SDXC-copy");
            return;
        }

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
