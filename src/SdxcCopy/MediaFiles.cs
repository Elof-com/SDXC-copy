namespace SdxcCopy;

/// <summary>
/// Vilka filtyper som räknas som bilder respektive video. Endast dessa
/// kopieras vid import — kamerornas hjälpfiler (t.ex. Canons .CTG-kataloger,
/// .THM-miniatyrer och andra fabrikats indexfiler) tas inte med.
/// </summary>
public static class MediaFiles
{
    public static readonly HashSet<string> StillImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Vanliga bildformat
        ".jpg", ".jpeg", ".jpe", ".heic", ".heif", ".hif",
        ".tif", ".tiff", ".png", ".bmp", ".webp", ".avif", ".jxl",
        // Råformat
        ".dng", ".cr2", ".cr3", ".crw",          // Canon
        ".nef", ".nrw",                          // Nikon
        ".arw", ".srf", ".sr2",                  // Sony
        ".orf",                                  // Olympus/OM System
        ".rw2", ".raw",                          // Panasonic
        ".raf",                                  // Fujifilm
        ".pef", ".ptx",                          // Pentax
        ".srw",                                  // Samsung
        ".x3f",                                  // Sigma
        ".gpr",                                  // GoPro
        ".3fr", ".fff",                          // Hasselblad
        ".iiq",                                  // Phase One
        ".mrw",                                  // Minolta
        ".mef", ".mos",                          // Mamiya/Leaf
        ".erf",                                  // Epson
        ".kdc", ".dcr",                          // Kodak
        ".rwl",                                  // Leica
    };

    public static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".m4v", ".avi",
        ".mts", ".m2ts", ".mxf",
        ".mpg", ".mpeg", ".wmv", ".webm",
        ".crm", ".braw",                         // Canon Cinema RAW Light, Blackmagic RAW
    };

    public static bool IsMediaFile(FileInfo file) =>
        StillImageExtensions.Contains(file.Extension) || VideoExtensions.Contains(file.Extension);
}
