using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SdxcCopy;

/// <summary>
/// Osynligt fönster som lyssnar på WM_DEVICECHANGE och rapporterar när en
/// ny volym (t.ex. ett SDXC-kort) dyker upp i Windows. Fönstret används
/// också för att köra kod på UI-tråden.
/// </summary>
public sealed class DriveWatcher : Form
{
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVTYP_VOLUME = 0x0002;

    public event Action<string>? DriveArrived;

    public DriveWatcher()
    {
        ShowInTaskbar = false;
        // Tvinga fram fönsterhandtaget så att meddelanden tas emot
        // trots att fönstret aldrig visas.
        _ = Handle;
    }

    protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_DEVICECHANGE &&
            m.WParam.ToInt64() == DBT_DEVICEARRIVAL &&
            m.LParam != IntPtr.Zero &&
            Marshal.ReadInt32(m.LParam, 4) == DBT_DEVTYP_VOLUME)
        {
            // DEV_BROADCAST_VOLUME: dbcv_unitmask på offset 12, en bit per enhetsbokstav.
            var unitMask = Marshal.ReadInt32(m.LParam, 12);
            for (var i = 0; i < 26; i++)
            {
                if ((unitMask & (1 << i)) != 0)
                    DriveArrived?.Invoke($"{(char)('A' + i)}:\\");
            }
        }
        base.WndProc(ref m);
    }
}
