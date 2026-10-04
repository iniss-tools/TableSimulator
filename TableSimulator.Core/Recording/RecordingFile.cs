using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Hlavicka zaznamu: kedy vznikol a nastavenie liniek a tabul v tom case (pri prehrati inde sa podla neho doplnia).
/// </summary>
/// <param name="Format">vzdy <see cref="RecordingFile.FormatName" /></param>
/// <param name="Version">verzia formatu</param>
/// <param name="Created">zaciatok zaznamu</param>
/// <param name="Config">linky a tabule simulatora pri zaciatku zaznamu</param>
/// <param name="Note">poznamka obsluhy</param>
public sealed record RecordingHeader(string Format, int Version, DateTimeOffset Created, SimulatorConfig Config, string? Note);

/// <summary>
/// Kus prevadzky na linke - bajty tak, ako prisli (nie cele spravy), aby prehratie preslo tym istym skladanim sprav.
/// </summary>
/// <param name="Time">cas od zaciatku zaznamu [s]</param>
/// <param name="Line">cislo linky</param>
/// <param name="Direction">od INISSu (prehrava sa) alebo odpoved tabule (len na porovnanie)</param>
/// <param name="Bytes">bajty</param>
public sealed record RecordingEntry(double Time, int Line, FrameDirection Direction, byte[] Bytes);

/// <summary>
/// Zaznam komunikacie (<c>.tsrec</c>) - JSON Lines v UTF-8: prvy riadok hlavicka, potom jeden riadok na kus prevadzky
/// <c>{"t":1.234,"line":1,"rx":"0185…"}</c> (od INISSu) alebo <c>{"t":…,"line":1,"tx":"06"}</c> (odpoved tabule).
/// </summary>
public sealed class RecordingFile
{
    /// <summary>Oznacenie formatu v hlavicke.</summary>
    public const string FormatName = "TableSimulator.Recording";

    /// <summary>Verzia formatu.</summary>
    public const int Version = 1;

    /// <summary>Pripona suboru.</summary>
    public const string Extension = ".tsrec";

    /// <summary>JSON hlavicky - rovnake nazvy ako v nastaveni simulatora.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public RecordingFile(RecordingHeader header, IReadOnlyList<RecordingEntry> entries)
    {
        Header = header;
        Entries = entries;
    }

    /// <summary>Hlavicka.</summary>
    public RecordingHeader Header { get; }

    /// <summary>Prevadzka v poradi casu.</summary>
    public IReadOnlyList<RecordingEntry> Entries { get; }

    /// <summary>Dlzka zaznamu [s].</summary>
    public double Duration => Entries.Count == 0 ? 0 : Entries[^1].Time;

    /// <summary>Linky, na ktorych je prevadzka.</summary>
    public IReadOnlyList<int> Lines => Entries.Select(e => e.Line).Distinct().Order().ToList();

    /// <summary>Riadok hlavicky.</summary>
    public static string HeaderLine(RecordingHeader header) => JsonSerializer.Serialize(header, JsonOptions);

    /// <summary>Riadok kusu prevadzky.</summary>
    public static string EntryLine(RecordingEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var key = entry.Direction == FrameDirection.Received ? "rx" : "tx";
        return string.Create(CultureInfo.InvariantCulture,
            $"{{\"t\":{entry.Time:0.000},\"line\":{entry.Line},\"{key}\":\"{Convert.ToHexString(entry.Bytes)}\"}}");
    }

    /// <summary>Zapise cely zaznam.</summary>
    public void Write(Stream stream)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine(HeaderLine(Header));
        foreach (var entry in Entries)
            writer.WriteLine(EntryLine(entry));
    }

    /// <summary>Nacita zaznam zo suboru.</summary>
    public static RecordingFile Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    /// <summary>
    /// Precita zaznam. Zly riadok prevadzky sa preskoci; bez platnej hlavicky <see cref="FormatException" />.
    /// </summary>
    public static RecordingFile Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true);
        var header = ParseHeader(reader.ReadLine());

        var entries = new List<RecordingEntry>();
        while (reader.ReadLine() is { } line)
        {
            if (ParseEntry(line) is { } entry)
                entries.Add(entry);
        }

        entries.Sort((a, b) => a.Time.CompareTo(b.Time));
        return new RecordingFile(header, entries);
    }

    /// <summary>Len hlavicka (zoznam zaznamov).</summary>
    public static RecordingHeader ReadHeader(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true);
        return ParseHeader(reader.ReadLine());
    }

    private static RecordingHeader ParseHeader(string? line)
    {
        RecordingHeader? header = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(line))
                header = JsonSerializer.Deserialize<RecordingHeader>(line, JsonOptions);
        }
        catch (JsonException e)
        {
            throw new FormatException("Recording header is not valid JSON.", e);
        }

        if (header == null || header.Format != FormatName)
            throw new FormatException("Not a TableSimulator recording.");
        if (header.Version > Version)
            throw new FormatException(string.Create(CultureInfo.InvariantCulture, $"Recording version {header.Version} is newer than supported {Version}."));
        return header;
    }

    private static RecordingEntry? ParseEntry(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var time = root.GetProperty("t").GetDouble();
            var number = root.GetProperty("line").GetInt32();
            if (root.TryGetProperty("rx", out var rx))
                return new RecordingEntry(time, number, FrameDirection.Received, Convert.FromHexString(rx.GetString() ?? ""));
            if (root.TryGetProperty("tx", out var tx))
                return new RecordingEntry(time, number, FrameDirection.Sent, Convert.FromHexString(tx.GetString() ?? ""));
            return null;
        }
        catch (Exception e) when (e is JsonException or FormatException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }
}
