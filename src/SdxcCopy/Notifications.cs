using Microsoft.Toolkit.Uwp.Notifications;

namespace SdxcCopy;

/// <summary>Windows-aviseringar. Klick och knappar hanteras i TrayApplicationContext.</summary>
public static class Notifications
{
    public static void CardDetected(string driveRoot, string cameraName)
    {
        new ToastContentBuilder()
            .AddArgument("action", "import")
            .AddArgument("drive", driveRoot)
            .AddText($"Kort från {cameraName} upptäckt")
            .AddText($"Enhet {driveRoot} — starta import?")
            .AddButton(new ToastButton()
                .SetContent("Starta import")
                .AddArgument("action", "import")
                .AddArgument("drive", driveRoot))
            .AddButton(new ToastButtonDismiss("Inte nu"))
            .Show();
    }

    public static void UnknownCamera(string driveRoot, string cameraName)
    {
        new ToastContentBuilder()
            .AddArgument("action", "newcamera")
            .AddArgument("drive", driveRoot)
            .AddText($"Ny kamera: {cameraName}")
            .AddText($"Kortet i {driveRoot} kommer från en kamera som inte lagts till ännu.")
            .AddButton(new ToastButton()
                .SetContent("Lägg till kamera")
                .AddArgument("action", "newcamera")
                .AddArgument("drive", driveRoot))
            .AddButton(new ToastButtonDismiss("Inte nu"))
            .Show();
    }

    public static void Info(string title, string text)
    {
        new ToastContentBuilder()
            .AddText(title)
            .AddText(text)
            .Show();
    }
}
