using System.Drawing;
using System.Windows.Forms;

namespace SdxcCopy;

/// <summary>
/// Guiden för att lägga till en ny kamera, och redigering av en befintlig.
/// Här kopplas kameran till sin grundkatalog och sitt mappmönster
/// (katalogstrukturen som skapas under grundkatalogen).
/// </summary>
public sealed class CameraForm : Form
{
    private readonly TextBox _nameBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly TextBox _directoryBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true };
    private readonly TextBox _patternBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };

    public CameraConfig Camera { get; }

    public CameraForm(CameraConfig camera, bool isNew)
    {
        Camera = camera;

        Text = isNew ? "Ny kamera — SDXC-copy" : "Ändra kamera — SDXC-copy";
        Icon = AppIcon.Get();
        // Skalning efter textstorlek så att fönstret fungerar på skärmar
        // med 125/150 % DPI-skalning.
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        TopMost = true;
        // Höjden beräknas från innehållet i stället för ett gissat fast mått,
        // så att inga fält kan hamna utanför fönstret när text radbryts.
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(620, 0);
        // Extra utrymme i botten — autosizade dialoger kan annars klippa
        // sista raden med några pixlar vid DPI-skalning.
        Padding = new Padding(0, 0, 0, 20);

        _nameBox.Text = camera.DisplayName;
        _directoryBox.Text = camera.BaseDirectory;
        _patternBox.Text = camera.FolderPattern;

        var browseButton = new Button { Text = "Bläddra…", AutoSize = true };
        browseButton.Click += (_, _) => BrowseDirectory();

        var okButton = new Button { Text = "OK", AutoSize = true, MinimumSize = new Size(96, 0) };
        okButton.Click += (_, _) => TrySave();
        var cancelButton = new Button
        {
            Text = "Avbryt",
            AutoSize = true,
            MinimumSize = new Size(96, 0),
            DialogResult = DialogResult.Cancel,
        };
        AcceptButton = okButton;
        CancelButton = cancelButton;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(16, 12, 16, 12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        void AddFullRow(Control control)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(control, 0, layout.RowCount);
            layout.SetColumnSpan(control, 2);
            layout.RowCount++;
        }

        Label MakeLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 3),
        };

        AddFullRow(MakeLabel("Kamerans namn:"));
        AddFullRow(_nameBox);
        AddFullRow(MakeLabel("Grundkatalog (dit bilderna kopieras):"));

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_directoryBox, 0, layout.RowCount);
        layout.Controls.Add(browseButton, 1, layout.RowCount);
        layout.RowCount++;

        AddFullRow(MakeLabel("Mappmönster (katalogstrukturen under grundkatalogen):"));
        AddFullRow(_patternBox);
        AddFullRow(new Label
        {
            Text = FolderPattern.PlaceholderHelp + "\nStandard: " + FolderPattern.Default,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 4, 0, 4),
        });

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 14, 0, 6),
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(okButton);
        AddFullRow(buttons);

        Controls.Add(layout);
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
