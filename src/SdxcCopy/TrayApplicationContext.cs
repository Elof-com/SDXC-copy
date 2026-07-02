using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows.Forms;

namespace SdxcCopy;

/// <summary>
/// Programmets nav: ikonen i systemfältet, bevakningen av kortinsläpp,
/// avisering → bekräftelse → import, samt guiden för nya kameror.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly DriveWatcher _driveWatcher;
    private readonly AppConfig _config;
    private readonly HashSet<string> _activeImports = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _importLock = new();
    private bool _settingsOpen;

    public TrayApplicationContext()
    {
        _config = ConfigStore.Load();

        _driveWatcher = new DriveWatcher();
        _driveWatcher.DriveArrived += root => Task.Run(() => AnnounceCard(root));

        var menu = new ContextMenuStrip();
        menu.Items.Add("Sök efter kort nu", null, (_, _) => Task.Run(ScanAllDrives));
        menu.Items.Add("Inställningar…", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Avsluta", null, (_, _) => ExitThread());

        _trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "SDXC-copy — bevakar SDXC-kort",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => OpenSettings();

        ToastNotificationManagerCompat.OnActivated += OnToastActivated;

        // Kort som redan sitter i när programmet startar.
        Task.Run(ScanAllDrives);
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _driveWatcher.Dispose();
        ToastNotificationManagerCompat.Uninstall();
        base.ExitThreadCore();
    }

    private void ScanAllDrives()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady || drive.DriveType == DriveType.Network)
                    continue;
            }
            catch (Exception)
            {
                continue;
            }
            AnnounceCard(drive.RootDirectory.FullName);
        }
    }

    /// <summary>Steg 1: ett kort har dykt upp — identifiera kameran och fråga användaren.</summary>
    private void AnnounceCard(string driveRoot)
    {
        var dcim = CameraIdentifier.FindDcim(driveRoot);
        if (dcim is null)
            return;

        lock (_importLock)
        {
            if (_activeImports.Contains(driveRoot))
                return;
        }

        var identity = CameraIdentifier.Identify(dcim);
        if (identity is null)
        {
            Notifications.Info(
                "Kort upptäckt",
                $"Kameran på {driveRoot} kunde inte identifieras (ingen EXIF-information hittades). Ingen import görs.");
            return;
        }

        var camera = _config.FindCamera(identity.Id);
        if (camera is null)
            Notifications.UnknownCamera(driveRoot, identity.DisplayName);
        else
            Notifications.CardDetected(driveRoot, camera.DisplayName);
    }

    /// <summary>Steg 2: användaren klickade i aviseringen.</summary>
    private void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        var args = ToastArguments.Parse(e.Argument);
        if (!args.TryGetValue("action", out string action) ||
            !args.TryGetValue("drive", out string driveRoot))
        {
            return;
        }

        switch (action)
        {
            case "import":
                Task.Run(() => RunImport(driveRoot));
                break;
            case "newcamera":
                _driveWatcher.BeginInvoke(() => AddNewCamera(driveRoot));
                break;
        }
    }

    /// <summary>Guiden för ny kamera. Körs på UI-tråden.</summary>
    private void AddNewCamera(string driveRoot)
    {
        // Kortet identifieras på nytt — det kan ha bytts sedan aviseringen visades.
        var dcim = CameraIdentifier.FindDcim(driveRoot);
        var identity = dcim is null ? null : CameraIdentifier.Identify(dcim);
        if (identity is null)
        {
            Notifications.Info("SDXC-copy", $"Inget identifierbart kamerakort hittades i {driveRoot} längre.");
            return;
        }

        if (_config.FindCamera(identity.Id) is null)
        {
            var camera = new CameraConfig
            {
                Id = identity.Id,
                DisplayName = identity.DisplayName,
                FolderPattern = FolderPattern.Default,
            };
            using var form = new CameraForm(camera, isNew: true);
            if (form.ShowDialog() != DialogResult.OK)
                return;

            _config.Cameras.Add(camera);
            ConfigStore.Save(_config);
        }

        // Kameran är tillagd — användarens OK i guiden räknas som bekräftelse.
        Task.Run(() => RunImport(driveRoot));
    }

    /// <summary>Steg 3: själva importen. Körs på bakgrundstråd.</summary>
    private void RunImport(string driveRoot)
    {
        lock (_importLock)
        {
            if (!_activeImports.Add(driveRoot))
                return;
        }

        try
        {
            var dcim = CameraIdentifier.FindDcim(driveRoot);
            var identity = dcim is null ? null : CameraIdentifier.Identify(dcim);
            if (dcim is null || identity is null)
            {
                Notifications.Info("Import avbruten", $"Inget identifierbart kamerakort hittades i {driveRoot}.");
                return;
            }

            var camera = _config.FindCamera(identity.Id);
            if (camera is null)
            {
                Notifications.UnknownCamera(driveRoot, identity.DisplayName);
                return;
            }

            if (!System.IO.Directory.Exists(camera.BaseDirectory))
            {
                Notifications.Info(
                    "Import avbruten",
                    $"Grundkatalogen {camera.BaseDirectory} går inte att nå. Ingenting kopierades — " +
                    "kortet är orört, så sätt i det igen när platsen är tillgänglig.");
                return;
            }

            // Förloppsfönster i stället för en "import startad"-avisering.
            ProgressForm? progressForm = null;
            _driveWatcher.Invoke(() =>
            {
                progressForm = new ProgressForm($"Importerar från {camera.DisplayName} ({driveRoot}) — SDXC-copy");
                progressForm.Show();
            });

            ImportResult result;
            try
            {
                result = Importer.Run(dcim, camera, (done, total, fileName) =>
                    progressForm?.UpdateProgress(done, total, fileName));
            }
            finally
            {
                _driveWatcher.BeginInvoke(() =>
                {
                    progressForm?.Close();
                    progressForm?.Dispose();
                });
            }

            var title = result.Failed == 0 ? "Import klar" : "Import klar med fel";
            var text = $"{camera.DisplayName}: {result.Summary()}";
            if (result.Errors.Count > 0)
                text += $"\nFörsta felet: {result.Errors[0]}";
            Notifications.Info(title, text);
        }
        catch (Exception ex)
        {
            Notifications.Info("Import misslyckades", $"{driveRoot}: {ex.Message}");
        }
        finally
        {
            lock (_importLock)
            {
                _activeImports.Remove(driveRoot);
            }
        }
    }

    private void OpenSettings()
    {
        if (_driveWatcher.InvokeRequired)
        {
            _driveWatcher.BeginInvoke(OpenSettings);
            return;
        }
        if (_settingsOpen)
            return;

        _settingsOpen = true;
        try
        {
            using var form = new SettingsForm(_config);
            form.ShowDialog();
        }
        finally
        {
            _settingsOpen = false;
        }
    }
}
