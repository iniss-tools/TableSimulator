using System.Globalization;
using System.Text.RegularExpressions;

namespace TableSimulator.Core.Archive;

/// <summary>Denny archiv najdeny v priecinkoch logov INISSu.</summary>
/// <param name="Kind">druh</param>
/// <param name="Date">den</param>
/// <param name="Path">subor</param>
/// <param name="Modified">posledna zmena (miestny cas)</param>
/// <param name="Length">velkost v bajtoch</param>
public sealed record ArchiveDay(ArchiveKind Kind, DateOnly Date, string Path, DateTime Modified, long Length);

/// <summary>
/// Denne archivy v priecinkoch logov INISSu: <c>TAB&lt;rrrr-mm-dd&gt;.XML</c> (obsah tabul) a
/// <c>REP&lt;rrrr-mm-dd&gt;.XML</c> (hlasenia) - tvar, ktory hlada aj INISSView. Rovnaky den vo viacerych priecinkoch
/// sa berie z novsieho suboru.
/// </summary>
public static partial class ArchiveFiles
{
    /// <summary>Dni archivu druhu <paramref name="kind" /> od najnovsieho.</summary>
    public static IReadOnlyList<ArchiveDay> Days(IReadOnlyList<string> folders, ArchiveKind kind)
    {
        ArgumentNullException.ThrowIfNull(folders);
        var prefix = kind == ArchiveKind.Tables ? "TAB" : "REP";
        var days = new Dictionary<DateOnly, ArchiveDay>();
        foreach (var folder in folders.Where(f => !string.IsNullOrWhiteSpace(f)))
        {
            FileInfo[] files;
            try
            {
                var directory = new DirectoryInfo(folder);
                if (!directory.Exists)
                    continue;
                files = directory.GetFiles(prefix + "*");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                continue;
            }

            foreach (var file in files)
            {
                if (DayName().Match(file.Name) is not { Success: true } match
                    || !string.Equals(match.Groups[1].Value, prefix, StringComparison.OrdinalIgnoreCase)
                    || !DateOnly.TryParseExact(match.Groups[2].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    continue;
                if (!days.TryGetValue(date, out var other) || file.LastWriteTimeUtc > other.Modified.ToUniversalTime())
                    days[date] = new ArchiveDay(kind, date, file.FullName, file.LastWriteTime, file.Length);
            }
        }

        return days.Values.OrderByDescending(d => d.Date).ToArray();
    }

    [GeneratedRegex(@"^(TAB|REP)(\d{4}-\d{2}-\d{2})\.xml$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DayName();
}
