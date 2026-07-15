using Microsoft.Win32;
using System.Windows.Forms;

namespace SdxcCopy;

/// <summary>
/// Autostart med Windows via Run-nyckeln i registret.
/// </summary>
public static class Autostart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "SDXC-copy";

    private static string CurrentCommand => $"\"{Application.ExecutablePath}\"";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) is not null;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
            key.SetValue(RunValueName, CurrentCommand);
        else
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    /// <summary>
    /// Uppdaterar registervärdet om programmet bytt namn eller flyttats
    /// (t.ex. vid uppgradering via winget) så att autostarten inte tyst
    /// slutar fungera.
    /// </summary>
    public static void RefreshPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key?.GetValue(RunValueName) is string command && command != CurrentCommand)
                key.SetValue(RunValueName, CurrentCommand);
        }
        catch (Exception)
        {
            // Registret oåtkomligt — autostarten får rättas via inställningarna.
        }
    }
}
