using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Archive;

/// <summary>
/// Cita denne archivy INISSu. Subor nema korenovy prvok - kazdy riadok je jeden zaznam <c>&lt;T …&gt;</c> alebo
/// <c>&lt;H … /&gt;</c> v kodovani windows-1250. Cita sa po riadkoch: neuplny posledny riadok (INISS prave zapisuje)
/// alebo poskodeny riadok sa preskoci a zarata. Archiv hlaseni INISSu 3.39 obsahuje aj riadky legendy vyluk
/// a odklonov (<c>; V12 text</c>), podla ktorej sa doplni text k atributom <c>vyl</c>, <c>vylo</c> a <c>od</c>.
/// </summary>
public static partial class ArchiveReader
{
    /// <summary>Zaznamy archivu tabul.</summary>
    public static ArchiveContent ReadTables(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var entries = new List<TableArchiveEntry>();
        var skipped = 0;
        foreach (var line in Lines(stream))
        {
            if (line.StartsWith(';') || line.StartsWith("<?", StringComparison.Ordinal))
                continue;
            if (Parse(line) is not { } element || !string.Equals(element.Name.LocalName, "T", StringComparison.Ordinal))
            {
                skipped++;
                continue;
            }

            entries.Add(new TableArchiveEntry(Time(element), Attribute(element, "s"), Train(element), Child(element, "tx"),
                element.Elements("t").Select(Table).ToArray()));
        }

        return new ArchiveContent(entries, [], skipped);
    }

    /// <summary>Zaznamy archivu hlaseni (s legendou vyluk a odklonov).</summary>
    public static ArchiveContent ReadReports(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var entries = new List<ReportArchiveEntry>();
        var legend = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var skipped = 0;
        foreach (var line in Lines(stream))
        {
            if (ReportLine(line, legend) is { } entry)
                entries.Add(entry);
            else if (!IsNote(line))
                skipped++;
        }

        return new ArchiveContent([], entries, skipped);
    }

    /// <summary>
    /// Jeden riadok archivu hlaseni (orezany): zaznam hlasenia, alebo null - riadok legendy (doplni ju), deklaracia
    /// alebo riadok, ktory sa neda precitat (<see cref="IsNote" /> ich rozlisi).
    /// </summary>
    internal static ReportArchiveEntry? ReportLine(string line, Dictionary<string, string> legend)
    {
        if (line.StartsWith(';'))
        {
            if (LegendLine().Match(line) is { Success: true } match)
                legend[match.Groups[1].Value] = match.Groups[2].Value.Trim();
            return null;
        }

        if (line.StartsWith("<?", StringComparison.Ordinal))
            return null;
        return Parse(line) is { } element && string.Equals(element.Name.LocalName, "H", StringComparison.Ordinal) ? Report(element, legend) : null;
    }

    /// <summary>Riadok, ktory nie je zaznam, ale ani chyba - komentar (legenda) alebo deklaracia XML.</summary>
    internal static bool IsNote(string line) => line.StartsWith(';') || line.StartsWith("<?", StringComparison.Ordinal);

    // atributy, ktore ma zaznam hlasenia ako vlastne polia
    private static readonly HashSet<string> ReportFields = ["d", "c", "u", "n", "a", "s", "t", "v", "jm", "dt", "kp", "ko", "zp", "zo", "x", "e"];

    private static ReportArchiveEntry Report(XElement element, Dictionary<string, string> legend)
    {
        var details = new List<KeyValuePair<string, string>>();
        foreach (var attribute in element.Attributes())
        {
            var name = attribute.Name.LocalName;
            if (ReportFields.Contains(name))
                continue;
            var value = attribute.Value;
            // vyluka a odklon su cisla kodov V<n> a O<n> z legendy na zaciatku dna
            var code = name switch { "vyl" or "vylo" => "V" + value, "od" => "O" + value, _ => null };
            if (code is not null && legend.TryGetValue(code, out var text))
                value = $"{code} {text}";
            else if (code is not null)
                value = code;
            details.Add(new KeyValuePair<string, string>(name, value));
        }

        return new ReportArchiveEntry(Time(element), Attribute(element, "u"), Attribute(element, "n"), Attribute(element, "a") == "true",
            Attribute(element, "s"), Train(element), Attribute(element, "kp"), Attribute(element, "ko"), Number(element, "zp"), Number(element, "zo"),
            Attribute(element, "x"), Attribute(element, "e"), details);
    }

    private static TableArchiveTable Table(XElement table) =>
        new(table.Elements("n").Select(n => n.Value).ToArray(),
            table.Elements("c").Select(c => new TableArchiveColumn(Attribute(c, "n"), Attribute(c, "s"))).ToArray(),
            Attribute(table, "lt"), Attribute(table, "r"));

    private static ArchiveTrain? Train(XElement element)
    {
        var number = Attribute(element, "v");
        if (number.Length == 0)
            return null;
        DateOnly? date = DateOnly.TryParseExact(Attribute(element, "dt"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
        return new ArchiveTrain(Attribute(element, "t"), number, Attribute(element, "jm"), date);
    }

    /// <summary>Cas zapisu - INISS 3.39 <c>HH:MM:SS</c>, INISS2 so zlomkami sekundy a pasmom.</summary>
    private static TimeOnly Time(XElement element)
    {
        var text = Attribute(element, "c");
        return TimeOnly.TryParseExact(text.Length > 8 ? text[..8] : text, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : default;
    }

    private static int? Number(XElement element, string name) =>
        int.TryParse(Attribute(element, name), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) ? n : null;

    private static string Attribute(XElement element, string name) => element.Attribute(name)?.Value ?? "";

    private static string Child(XElement element, string name) => element.Element(name)?.Value ?? "";

    /// <summary>Riadok ako XML; nezakodovany znak <c>&amp;</c> sa opravi, inak null.</summary>
    private static XElement? Parse(string line)
    {
        try
        {
            return XElement.Parse(line);
        }
        catch (XmlException)
        {
            try
            {
                return XElement.Parse(BareAmpersand().Replace(line, "&amp;"));
            }
            catch (XmlException)
            {
                return null;
            }
        }
    }

    private static IEnumerable<string> Lines(Stream stream)
    {
        using var reader = new StreamReader(stream, Encodings.Win1250, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        while (reader.ReadLine() is { } line)
        {
            var text = line.Trim();
            if (text.Length > 0)
                yield return text;
        }
    }

    [GeneratedRegex(@"&(?!(amp|lt|gt|quot|apos|#\d+|#x[0-9a-fA-F]+);)", RegexOptions.CultureInvariant)]
    private static partial Regex BareAmpersand();

    /// <summary>Riadok legendy: <c>;  V1000001 Bratislava hl.st. - Zohor</c>.</summary>
    [GeneratedRegex(@"^;\s+([VO]\d+)\s+(.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex LegendLine();

    /// <summary>Kodovanie archivov (windows-1250 bez deklaracie).</summary>
    public static Encoding Encoding => Encodings.Win1250;
}
