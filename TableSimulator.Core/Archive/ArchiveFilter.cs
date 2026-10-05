using System.Globalization;

namespace TableSimulator.Core.Archive;

/// <summary>
/// Vyber zaznamov archivu ako v INISSView: podla cisla vlaku a volneho textu (v ktoromkolvek udaji zaznamu, bez
/// ohladu na velkost pismen a diakritiku).
/// </summary>
public static class ArchiveFilter
{
    private static readonly CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions Options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>Zaznam archivu tabul vyhovuje.</summary>
    /// <param name="entry">zaznam</param>
    /// <param name="train">cislo vlaku (cast cisla); prazdne = kazdy</param>
    /// <param name="text">hladany text; prazdny = kazdy</param>
    public static bool Matches(TableArchiveEntry entry, string? train, string? text)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return MatchesTrain(entry.Train, train)
               && (string.IsNullOrWhiteSpace(text)
                   || Contains(text, [entry.Station, entry.Text, .. TrainTexts(entry.Train),
                       .. entry.Tables.SelectMany(t => t.Names.Concat(t.Columns.Select(c => c.Value)).Append(t.Logical))]));
    }

    /// <summary>Zaznam archivu hlaseni vyhovuje.</summary>
    public static bool Matches(ReportArchiveEntry entry, string? train, string? text)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return MatchesTrain(entry.Train, train)
               && (string.IsNullOrWhiteSpace(text)
                   || Contains(text, [entry.Kind, entry.Name, entry.Station, entry.Text, entry.Result, entry.ArrivalTrack, entry.DepartureTrack,
                       .. TrainTexts(entry.Train), .. entry.Details.Select(d => d.Value)]));
    }

    private static bool MatchesTrain(ArchiveTrain? entry, string? train) =>
        string.IsNullOrWhiteSpace(train) || entry is not null && entry.Number.Contains(train.Trim(), StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> TrainTexts(ArchiveTrain? train) => train is null ? [] : [train.Type, train.Number, train.Name];

    private static bool Contains(string text, IEnumerable<string> values)
    {
        var needle = text.Trim();
        return values.Any(v => !string.IsNullOrEmpty(v) && Compare.IndexOf(v, needle, Options) >= 0);
    }
}
