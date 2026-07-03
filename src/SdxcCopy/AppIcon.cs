using System.Drawing;
using System.Windows.Forms;

namespace SdxcCopy;

/// <summary>
/// Programmets ikon (app.ico, inbäddad i exe-filen via ApplicationIcon).
/// Används i systemfältet och på alla fönster.
/// </summary>
public static class AppIcon
{
    private static Icon? _icon;

    public static Icon Get() => _icon ??= Load();

    private static Icon Load()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch (Exception)
        {
            // T.ex. vid körning utan inbäddad ikon — standardikonen duger då.
            return SystemIcons.Application;
        }
    }
}
