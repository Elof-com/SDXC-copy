using System.Windows.Forms;

namespace SdxcCopy;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, name: @"Local\SdxcCopy_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "SDXC-copy körs redan. Leta efter ikonen i systemfältet vid klockan.",
                "SDXC-copy",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new TrayApplicationContext());
    }
}
