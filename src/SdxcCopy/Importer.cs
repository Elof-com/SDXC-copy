using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;

namespace SdxcCopy;

public sealed class ImportResult
{
    public int Copied;
    public int SkippedDuplicates;
    public int Renamed;
    public int Failed;
    public List<string> Errors { get; } = new();

    public string Summary()
    {
        var text = $"{Copied} filer kopierade, {SkippedDuplicates} hoppades över";
        if (Renamed > 0)
            text += $", {Renamed} fick nytt namn";
        if (Failed > 0)
            text += $", {Failed} misslyckades";
        return text + ".";
    }
}

/// <summary>
/// Kopierar allt under DCIM till kamerans grundkatalog enligt mappmönstret.
/// Grundprinciper: kortet öppnas strikt läsande och förändras aldrig;
/// inget i målet skrivs över; redan kopierade filer (samma namn och storlek
/// i målmappen) kopieras inte igen.
/// </summary>
public static class Importer
{
    private const int MaxReportedErrors = 20;

    public static ImportResult Run(string dcimPath, CameraConfig camera)
    {
        var result = new ImportResult();

        foreach (var file in new DirectoryInfo(dcimPath).EnumerateFiles("*", SearchOption.AllDirectories))
        {
            try
            {
                ImportFile(file, camera, result);
            }
            catch (Exception ex)
            {
                result.Failed++;
                if (result.Errors.Count < MaxReportedErrors)
                    result.Errors.Add($"{file.FullName}: {ex.Message}");
            }
        }
        return result;
    }

    private static void ImportFile(FileInfo file, CameraConfig camera, ImportResult result)
    {
        var date = GetCaptureDate(file);
        var targetDirectory = Path.Combine(camera.BaseDirectory, FolderPattern.Expand(camera.FolderPattern, date));
        System.IO.Directory.CreateDirectory(targetDirectory);

        var baseName = Path.GetFileNameWithoutExtension(file.Name);
        var extension = file.Extension;

        // Namn + storlek i målmappen avgör dubbletter. Vid kollision
        // (samma namn, annan storlek) provas "namn (2)", "namn (3)" osv.
        for (var attempt = 1; ; attempt++)
        {
            var candidateName = attempt == 1 ? file.Name : $"{baseName} ({attempt}){extension}";
            var target = new FileInfo(Path.Combine(targetDirectory, candidateName));

            if (!target.Exists)
            {
                File.Copy(file.FullName, target.FullName);
                result.Copied++;
                if (attempt > 1)
                    result.Renamed++;
                return;
            }

            if (target.Length == file.Length)
            {
                result.SkippedDuplicates++;
                return;
            }
        }
    }

    /// <summary>
    /// Fotograferingsdatum ur EXIF i första hand, videometadata i andra hand,
    /// filens ändringsdatum på kortet som reserv.
    /// </summary>
    private static DateTime GetCaptureDate(FileInfo file)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(file.FullName);

            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfd is not null &&
                (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var exifDate) ||
                 subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out exifDate)) &&
                IsPlausible(exifDate))
            {
                return exifDate;
            }

            var movieHeader = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            if (movieHeader is not null &&
                movieHeader.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var videoDate) &&
                IsPlausible(videoDate))
            {
                return videoDate;
            }
        }
        catch (Exception)
        {
            // Okänt filformat eller trasig metadata — använd filens datum.
        }

        return file.LastWriteTime;
    }

    // Skyddar mot nollställda kameraklockor och tomma metadatafält
    // (t.ex. QuickTime-epoken 1904).
    private static bool IsPlausible(DateTime date) => date.Year >= 1980;
}
