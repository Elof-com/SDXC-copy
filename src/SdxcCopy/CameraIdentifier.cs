using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace SdxcCopy;

public sealed record CameraIdentity(string Id, string DisplayName);

/// <summary>
/// Identifierar vilken kamera ett kort kommer ifrån genom att läsa
/// kameramodell och serienummer ur EXIF-datat i bilderna på kortet.
/// Alla filer öppnas strikt läsande — kortet förändras aldrig.
/// </summary>
public static class CameraIdentifier
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".heic", ".heif", ".tif", ".tiff", ".png",
        ".dng", ".cr2", ".cr3", ".crw", ".nef", ".nrw", ".arw", ".sr2",
        ".orf", ".rw2", ".raf", ".pef", ".srw", ".x3f", ".gpr",
    };

    // Så många bildfiler provas innan identifieringen ger upp.
    private const int MaxFilesToProbe = 50;

    /// <summary>Returnerar sökvägen till DCIM-mappen om enheten ser ut som ett kamerakort.</summary>
    public static string? FindDcim(string driveRoot)
    {
        try
        {
            var dcim = Path.Combine(driveRoot, "DCIM");
            return System.IO.Directory.Exists(dcim) ? dcim : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static CameraIdentity? Identify(string dcimPath)
    {
        IEnumerable<FileInfo> files;
        try
        {
            files = new DirectoryInfo(dcimPath)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(f => ImageExtensions.Contains(f.Extension))
                .Take(MaxFilesToProbe);
        }
        catch (Exception)
        {
            return null;
        }

        foreach (var file in files)
        {
            var identity = TryIdentifyFromFile(file.FullName);
            if (identity is not null)
                return identity;
        }
        return null;
    }

    private static CameraIdentity? TryIdentifyFromFile(string path)
    {
        IReadOnlyList<MetadataExtractor.Directory> directories;
        try
        {
            directories = ImageMetadataReader.ReadMetadata(path);
        }
        catch (Exception)
        {
            return null;
        }

        var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        var model = ifd0?.GetDescription(ExifDirectoryBase.TagModel)?.Trim();
        if (string.IsNullOrEmpty(model))
            return null;

        var make = ifd0?.GetDescription(ExifDirectoryBase.TagMake)?.Trim();
        var displayName = !string.IsNullOrEmpty(make) &&
                          !model.StartsWith(make, StringComparison.OrdinalIgnoreCase)
            ? $"{make} {model}"
            : model;

        var serial = FindSerialNumber(directories);
        var id = $"{displayName}|{serial ?? "utan-serienummer"}";
        return new CameraIdentity(id, displayName);
    }

    private static string? FindSerialNumber(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        // Standardtaggen BodySerialNumber i första hand.
        var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var serial = Clean(subIfd?.GetDescription(ExifDirectoryBase.TagBodySerialNumber));
        if (serial is not null)
            return serial;

        // Många kameror lägger serienumret i tillverkarens egna taggar i stället.
        foreach (var directory in directories)
        {
            foreach (var tag in directory.Tags)
            {
                if (tag.Name.Contains("Serial Number", StringComparison.OrdinalIgnoreCase))
                {
                    serial = Clean(tag.Description);
                    if (serial is not null)
                        return serial;
                }
            }
        }
        return null;
    }

    private static string? Clean(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value) || value.Trim('0') == "")
            return null;
        return value;
    }
}
