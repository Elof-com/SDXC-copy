using System.Drawing;
using System.Windows.Forms;

namespace SdxcCopy;

/// <summary>
/// Förloppsfönstret som visas medan en import kopierar filer.
/// Uppdateras säkert från bakgrundstråden via UpdateProgress.
/// </summary>
public sealed class ProgressForm : Form
{
    private readonly Label _statusLabel = new()
    {
        Dock = DockStyle.Top,
        AutoEllipsis = true,
        Height = 24,
        Text = "Förbereder…",
    };

    private readonly ProgressBar _bar = new()
    {
        Dock = DockStyle.Top,
        Height = 24,
        Minimum = 0,
    };

    public ProgressForm(string title)
    {
        Text = title;
        // Skalning efter textstorlek så att fönstret fungerar på skärmar
        // med 125/150 % DPI-skalning.
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        // Ingen stängningsruta — fönstret stängs av programmet när importen är klar.
        ControlBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        ClientSize = new Size(520, 92);

        var layout = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 14, 16, 14) };
        // Dock=Top staplar i omvänd tilläggsordning: lägg listen först så
        // att etiketten hamnar överst.
        layout.Controls.Add(_bar);
        layout.Controls.Add(_statusLabel);
        Controls.Add(layout);
    }

    public void UpdateProgress(int done, int total, string fileName)
    {
        if (IsDisposed || !IsHandleCreated)
            return;
        try
        {
            BeginInvoke(() =>
            {
                if (IsDisposed)
                    return;
                if (total > 0)
                {
                    _bar.Maximum = total;
                    _bar.Value = Math.Min(done, total);
                }
                _statusLabel.Text = $"Fil {done} av {total} — {fileName}";
            });
        }
        catch (Exception)
        {
            // Fönstret hann stängas — förloppet behöver inte visas längre.
        }
    }
}
