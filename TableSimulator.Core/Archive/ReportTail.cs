namespace TableSimulator.Core.Archive;

/// <summary>Co pribudlo v sledovanom archive hlaseni.</summary>
/// <param name="Reset">sleduje sa iny subor (novy den) alebo sa subor skratil - predchadzajuce zaznamy neplatia</param>
/// <param name="Added">nove zaznamy v poradi suboru (pri <paramref name="Reset" /> vsetky zaznamy suboru)</param>
public sealed record ReportTailUpdate(bool Reset, IReadOnlyList<ReportArchiveEntry> Added)
{
    public bool IsEmpty => !Reset && Added.Count == 0;
}

/// <summary>
/// Priebezne citanie denneho archivu hlaseni INISSu (monitor hlaseni): pamata si, kolko bajtov sa uz precitalo, a cita
/// len pridane cele riadky - INISS zaznamy len pripisuje, riadky konci CR LF a posledny moze byt rozpisany. Legenda
/// vyluk a odklonov sa drzi od zaciatku suboru. Nie je bezpecne pre viac vlakien naraz.
/// </summary>
public sealed class ReportTail
{
    private readonly List<ReportArchiveEntry> _entries = [];
    private readonly Dictionary<string, string> _legend = new(StringComparer.OrdinalIgnoreCase);
    private long _offset;

    // zaciatok suboru - prepisany subor (iny zaciatok) sa cita znova, aj keby nebol kratsi
    private byte[] _head = [];
    private const int HeadLength = 256;

    /// <summary>Sledovany den; null = ziadny archiv hlaseni.</summary>
    public ArchiveDay? Day { get; private set; }

    /// <summary>Zaznamy sledovaneho dna v poradi suboru.</summary>
    public IReadOnlyList<ReportArchiveEntry> Entries => _entries;

    /// <summary>Riadky, ktore sa nedali precitat.</summary>
    public int Skipped { get; private set; }

    /// <summary>
    /// Doplni zaznamy z <paramref name="day" /> (najnovsi archiv hlaseni); iny subor nez doteraz sa cita od zaciatku.
    /// Nezmeneny subor (rovnaka velkost) sa neotvara.
    /// </summary>
    /// <exception cref="IOException">subor sa neda precitat</exception>
    public ReportTailUpdate Update(ArchiveDay? day)
    {
        if (day is null)
        {
            if (Day is null)
                return new ReportTailUpdate(false, []);
            Clear();
            Day = null;
            return new ReportTailUpdate(true, []);
        }

        var reset = false;
        if (Day is null || !string.Equals(Day.Path, day.Path, StringComparison.OrdinalIgnoreCase))
        {
            Clear();
            reset = true;
        }

        Day = day;
        if (!reset && day.Length == _offset)
            return new ReportTailUpdate(false, []);

        using var stream = new FileStream(day.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length < _offset || !SameHead(stream))
        {
            // subor sa skratil alebo ho INISS prepisal - znova od zaciatku
            Clear();
            reset = true;
        }

        var start = _entries.Count;
        if (stream.Length > _offset)
        {
            stream.Seek(_offset, SeekOrigin.Begin);
            var buffer = new byte[stream.Length - _offset];
            stream.ReadExactly(buffer);
            var end = Array.LastIndexOf(buffer, (byte)'\n');
            if (end >= 0)
            {
                // windows-1250 je jednobajtove - delenie na riadky po bajtoch nerozdeli znak
                foreach (var raw in ArchiveReader.Encoding.GetString(buffer, 0, end + 1).Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.Length == 0)
                        continue;
                    if (ArchiveReader.ReportLine(line, _legend) is { } entry)
                        _entries.Add(entry);
                    else if (!ArchiveReader.IsNote(line))
                        Skipped++;
                }

                _offset += end + 1;
            }

            if (_head.Length < HeadLength && _offset > _head.Length)
            {
                stream.Seek(0, SeekOrigin.Begin);
                _head = new byte[(int)Math.Min(HeadLength, _offset)];
                stream.ReadExactly(_head);
            }
        }

        return new ReportTailUpdate(reset, reset ? _entries.ToArray() : _entries.Skip(start).ToArray());
    }

    private bool SameHead(FileStream stream)
    {
        if (_head.Length == 0)
            return true;
        if (stream.Length < _head.Length)
            return false;
        var head = new byte[_head.Length];
        stream.Seek(0, SeekOrigin.Begin);
        stream.ReadExactly(head);
        return head.AsSpan().SequenceEqual(_head);
    }

    private void Clear()
    {
        _head = [];
        _entries.Clear();
        _legend.Clear();
        _offset = 0;
        Skipped = 0;
    }
}
