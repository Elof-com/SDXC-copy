using System.Windows.Forms;

namespace SdxcCopy;

/// <summary>
/// Guiden för att lägga till en ny kamera, och redigering av en befintlig.
/// Här kopplas kameran till sin grundkatalog och sitt mappmönster.
/// </summary>
public sealed class CameraForm : Form
{
    private readonly TextBox _nameBox = new() { Width = 340 };
    private readonly TextBox _directoryBox = new() { Width = 260, ReadOnly = true };
    private readonly TextBox _patternBox = new() { Width = 340 };

    public CameraConfig Camera { get; }

    public CameraForm(CameraConfig camera, bool isNew)
    {
        Camera = camera;

        Text = isNew ? "Ny kamera — SDXC-copy" : "Ändra kamera — SDXC-copy";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new System.Drawing.Size(400, 240);
        ShowInTaskbar = true;
        TopMost = true;

        _nameBox.Text = camera.DisplayName;
        _directoryBox.Text = camera.BaseDirectory;
        _patternBox.Text = camera.FolderPattern;

        var browseButton = new Button { Text = "Bläddra…", Width = 74 };
        browseButton.Click += (_, _) => BrowseDirectory();

        var okButton = new Button { Text = "OK", Width = 90, DialogResult = DialogResult.None };
        okButton.Click += (_, _) => TrySave();
        var cancelButton = new Button { Text = "Avbryt", Width = 90, DialogResult = DialogResult.Cancel };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        void AddRow(string label, Control control, Control? extra = null)
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 8, 0, 2) });
            layout.SetColumnSpan(layout.Controls[^1], extra is null ? 2 : 1);
            if (extra is not null)
                layout.Controls.Add(new Label { Text = "", AutoSize = true });
            layout.Controls.Add(control);
            if (extra is not null)
                layout.Controls.Add(extra);
            else
                layout.SetColumnSpan(control, 2);
        }

        AddRow("Kamerans namn:", _nameBox);
        AddRow("Grundkatalog (dit bilderna kopieras):", _directoryBox, browseButton);
        AddRow("Mappmönster under grundkatalogen:", _patternBox);

        layout.Controls.Add(new Label
        {
            Text = FolderPattern.PlaceholderHelp,
            AutoSize = true,
            ForeColor = System.Drawing.SystemColors.GrayText,
            Margin = new Padding(0, 2, 0, 8),
        });
        layout.SetColumnSpan(layout.Controls[^1], 2);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 42,
            Padding = new Padding(8),
        };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);

        Controls.Add(layout);
        Controls.Add(buttons);
    }

    private void BrowseDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Välj kamerans grundkatalog — dit bilderna kopieras.",
            UseDescriptionForTitle = true,
            SelectedPath = System.IO.Directory.Exists(_directoryBox.Text) ? _directoryBox.Text : "",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _directoryBox.Text = dialog.SelectedPath;
    }

    private void TrySave()
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            MessageBox.Show(this, "Ange ett namn på kameran.", "SDXC-copy");
            return;
        }
        if (!System.IO.Directory.Exists(_directoryBox.Text))
        {
            MessageBox.Show(this, "Välj en grundkatalog som finns.", "SDXC-copy");
            return;
        }
        if (!FolderPattern.IsValid(_patternBox.Text, out var patternError))
        {
            MessageBox.Show(this, patternError, "SDXC-copy");
            return;
        }

        Camera.DisplayName = _nameBox.Text.Trim();
        Camera.BaseDirectory = _directoryBox.Text;
        Camera.FolderPattern = _patternBox.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }
}
