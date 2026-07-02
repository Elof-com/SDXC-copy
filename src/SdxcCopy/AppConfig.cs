using System.Text.Json;

namespace SdxcCopy;

public sealed class CameraConfig
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string BaseDirectory { get; set; } = "";
    public string FolderPattern { get; set; } = SdxcCopy.FolderPattern.Default;
}

public sealed class AppConfig
{
    public List<CameraConfig> Cameras { get; set; } = new();

    public CameraConfig? FindCamera(string id) =>
        Cameras.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));
}

/// <summary>
/// Läser och sparar inställningarna i %APPDATA%\SDXC-copy\config.json.
/// Inställningar lagras alltid på datorn, aldrig på minneskortet.
/// </summary>
public static class ConfigStore
{
    public static string ConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SDXC-copy");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), JsonOptions);
                if (loaded is not null)
                    return loaded;
            }
        }
        catch (Exception)
        {
            // Oläsbar konfiguration: starta med tom i stället för att krascha.
        }
        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }
}
