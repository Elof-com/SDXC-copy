namespace SdxcCopy;

/// <summary>
/// Mappmönstret som styr strukturen under kamerans grundkatalog.
/// Platshållare: {ÅÅÅÅ} = år, {MM} = månad, {DD} = dag.
/// </summary>
public static class FolderPattern
{
    public const string Default = "{ÅÅÅÅ}/{MM}/{ÅÅÅÅ}-{MM}-{DD}";

    public const string PlaceholderHelp = "{ÅÅÅÅ} = år, {MM} = månad, {DD} = dag. Skriv / mellan mappnivåer.";

    public static string Expand(string pattern, DateTime date)
    {
        var expanded = pattern
            .Replace("{ÅÅÅÅ}", date.Year.ToString("D4"))
            .Replace("{MM}", date.Month.ToString("D2"))
            .Replace("{DD}", date.Day.ToString("D2"));
        return expanded.Replace('/', Path.DirectorySeparatorChar)
                       .Replace('\\', Path.DirectorySeparatorChar);
    }

    public static bool IsValid(string pattern, out string error)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            error = "Mappmönstret får inte vara tomt.";
            return false;
        }

        var expanded = Expand(pattern, new DateTime(2026, 1, 2));
        if (Path.IsPathRooted(expanded))
        {
            error = "Mappmönstret får inte börja med \\ eller en enhetsbokstav.";
            return false;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var segment in expanded.Split(Path.DirectorySeparatorChar))
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
            {
                error = "Mappmönstret innehåller en tom mappnivå eller \"..\".";
                return false;
            }
            if (segment.IndexOfAny(invalidChars) >= 0)
            {
                error = $"Mappnivån \"{segment}\" innehåller tecken som inte är tillåtna i mappnamn.";
                return false;
            }
        }

        error = "";
        return true;
    }
}
