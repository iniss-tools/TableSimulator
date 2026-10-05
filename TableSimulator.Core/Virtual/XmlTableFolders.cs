using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Xml;

namespace TableSimulator.Core.Virtual;

/// <summary>Subor XML tabule najdeny v sledovanych priecinkoch.</summary>
/// <param name="Name">meno bez pripony (hodnota <c>SAVE_XML</c> v TPhysic)</param>
/// <param name="Path">cesta k suboru, z ktoreho je obsah</param>
/// <param name="Modified">cas poslednej zmeny suboru (miestny)</param>
/// <param name="Table">posledny uspesne precitany obsah; null, ak sa este nepodarilo precitat</param>
/// <param name="Error">preco sa subor opakovane neda precitat; null = v poriadku</param>
public sealed record XmlTableSource(string Name, string Path, DateTime Modified, XmlTable? Table, string? Error)
{
    /// <summary>Obsah uz nie je platny (INISS asi nebezi).</summary>
    public bool IsStale(DateTime now) => Table?.IsStale(now) ?? false;
}

/// <summary>
/// XML tabul v priecinkoch logov INISSu (<c>PathNames\LogPath</c>). Pri kazdom prechode precita zmenene subory;
/// rovnomenny subor vo viacerych priecinkoch sa berie novsi (ako INISSView pri dvoch priecinkoch). Denne archivy
/// <c>TAB*.XML</c> a <c>REP*.XML</c> a ine XML nez tabule vynecha. INISS subor prepisuje - neuplny subor sa skusi
/// znova a chyba sa ohlasi az po niekolkych neuspesnych pokusoch, dovtedy plati posledny dobry obsah.
/// </summary>
/// <remarks><see cref="Poll" /> vola jedno vlakno; zoznam zdrojov sa da citat odkialkolvek.</remarks>
public sealed partial class XmlTableFolders
{
    /// <summary>Kolko neuspesnych pokusov o precitanie suboru prejde, kym sa ohlasi chyba.</summary>
    public const int FailuresBeforeError = 3;

    private const string Extension = ".xml";

    private volatile ImmutableSortedDictionary<string, XmlTableSource> _sources = ImmutableSortedDictionary.Create<string, XmlTableSource>(StringComparer.OrdinalIgnoreCase);
    // co sa zo suboru uz precitalo (aj subory, ktore nie su tabulou) - nezmeneny subor sa necita znova
    private readonly Dictionary<string, FileStamp> _read = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _failures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _stale = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Najdene tabule podla mena.</summary>
    public IReadOnlyList<XmlTableSource> Sources => _sources.Values.ToArray();

    /// <summary>Tabula s menom (bez pripony); null, ak v priecinkoch nie je.</summary>
    public XmlTableSource? Find(string name) => _sources.GetValueOrDefault(name);

    /// <summary>
    /// Prejde priecinky a vrati mena tabul, ktore sa zmenili: novy obsah, pribudnutie, zmiznutie, chyba citania
    /// alebo uplynutie platnosti obsahu.
    /// </summary>
    /// <param name="folders">priecinky v poradi priority (pri rovnakom case zmeny vyhra skorsi)</param>
    /// <param name="now">miestny cas - na platnost obsahu</param>
    public IReadOnlyList<string> Poll(IReadOnlyList<string> folders, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(folders);
        var changed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sources = _sources.ToBuilder();
        var files = NewestFiles(folders);

        foreach (var name in sources.Keys.Where(n => !files.ContainsKey(n)).ToArray())
        {
            sources.Remove(name);
            changed.Add(name);
        }

        foreach (var name in _read.Keys.Concat(_failures.Keys).Concat(_stale.Keys).Where(n => !files.ContainsKey(n)).ToArray())
        {
            _read.Remove(name);
            _failures.Remove(name);
            _stale.Remove(name);
        }

        foreach (var (name, file) in files)
        {
            var stamp = new FileStamp(file.FullName, file.LastWriteTime, file.Length);
            if (_read.TryGetValue(name, out var known) && known == stamp)
                continue;
            switch (Load(file))
            {
                case { Table: { } table }:
                    sources[name] = new XmlTableSource(name, file.FullName, file.LastWriteTime, table, null);
                    _read[name] = stamp;
                    _failures.Remove(name);
                    changed.Add(name);
                    break;
                case { Error: { } error }:
                    // nezapise sa do _read - pri dalsom prechode sa subor skusi znova
                    var failures = _failures[name] = _failures.GetValueOrDefault(name) + 1;
                    var previous = sources.GetValueOrDefault(name);
                    if (failures >= FailuresBeforeError && previous?.Error != error)
                    {
                        sources[name] = previous is null
                            ? new XmlTableSource(name, file.FullName, file.LastWriteTime, null, error)
                            : previous with { Error = error };
                        changed.Add(name);
                    }

                    break;
                default:
                    // ine XML nez tabula
                    _read[name] = stamp;
                    _failures.Remove(name);
                    if (sources.Remove(name))
                        changed.Add(name);
                    break;
            }
        }

        foreach (var source in sources.Values)
        {
            var stale = source.IsStale(now);
            if (_stale.TryGetValue(source.Name, out var was) && was != stale)
                changed.Add(source.Name);
            _stale[source.Name] = stale;
        }

        _sources = sources.ToImmutable();
        return changed.ToArray();
    }

    /// <summary>Najnovsi subor XML kazdeho mena v priecinkoch (bez dennych archivov).</summary>
    private static Dictionary<string, FileInfo> NewestFiles(IReadOnlyList<string> folders)
    {
        var files = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders.Where(f => !string.IsNullOrWhiteSpace(f)))
        {
            IEnumerable<FileInfo> found;
            try
            {
                var directory = new DirectoryInfo(folder);
                if (!directory.Exists)
                    continue;
                found = directory.EnumerateFiles("*" + Extension).ToArray();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // nedostupny sietovy priecinok - skusi sa pri dalsom prechode
                continue;
            }

            foreach (var file in found)
            {
                var name = Path.GetFileNameWithoutExtension(file.Name);
                if (!string.Equals(file.Extension, Extension, StringComparison.OrdinalIgnoreCase) || ArchiveName().IsMatch(name))
                    continue;
                if (!files.TryGetValue(name, out var other) || file.LastWriteTimeUtc > other.LastWriteTimeUtc)
                    files[name] = file;
            }
        }

        return files;
    }

    private static (XmlTable? Table, string? Error) Load(FileInfo file)
    {
        try
        {
            using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return (XmlTableReader.Read(stream), null);
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return (null, e.Message);
        }
    }

    /// <summary>Denne archivy tabul a hlaseni (<c>TAB2026-09-13</c>, <c>REP2026-09-13</c>).</summary>
    [GeneratedRegex(@"^(TAB|REP)\d{4}-\d{2}-\d{2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ArchiveName();

    private readonly record struct FileStamp(string Path, DateTime Modified, long Length);
}
