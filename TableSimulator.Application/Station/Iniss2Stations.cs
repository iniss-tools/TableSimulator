using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using ToolsCore.Iniss2;
using ToolsCore.Iniss2.Devices;
using ToolsCore.Iniss2.Grafikon;
using ToolsCore.Iniss2.Tables;
using ContractAlign = TableSimulator.Contracts.Faces.FaceAlign;

namespace TableSimulator.Application.Station;

/// <summary>
/// Linky a tabule servera INISS2 v rovnakom tvare ako instalacia INISS 3.39: grafikony z DirList.xml, linky z CommLines.xml
/// a tabule z Tables.xml a TCatalog.xml kazdej stanice. Linky maju v INISS2 textovy kluc, cislo im da poradie v subore.
/// </summary>
public static partial class Iniss2Stations
{
    /// <summary>Adresa simulatora v presmerovanej linke - simulator cita subory servera, bezi teda na tom istom pocitaci.</summary>
    private const string LocalHost = "127.0.0.1";

    /// <summary>Protokol, pri ktorom o triede linky rozhoduju jej tabule.</summary>
    private const string ElektrocasProtocol = "LCD";

    /// <summary>Trieda linky s tabulami LCD, FERS a LCD1/ERP (rovnaky protokol, iny vyrobca).</summary>
    private const int LcdClass = 1, FersClass = 3, Lcd1Class = 5;

    /// <summary>Bodov na znak znakovych tabul - INISS2 ma stlpce v znakoch, INISS 3.39 aj simulator v bodoch.</summary>
    private const int CharWidth = 8;

    /// <summary>Druhy tabul so stlpcami predlohy v znakoch; ELEN ich ma v bodoch ako INISS 3.39.</summary>
    private static readonly HashSet<string> CharacterClasses = new(StringComparer.Ordinal) { "LCD", "LCD1", "LCD2", "ERP", "ERP2", "FERS", "ERP_ELEN" };

    /// <summary>Triedy liniek ustredni AK Signal v simulatore (INISS 3.39 ich nepozna).</summary>
    private const int AkSignalClass = 131, AkSignalUhClass = 132;

    private static readonly Dictionary<string, int> ProtocolClasses = new(StringComparer.Ordinal)
    {
        ["ELEN"] = 4, ["ITP"] = 130, [Iniss2Devices.AkSignal] = AkSignalClass, [Iniss2Devices.AkSignalUh] = AkSignalUhClass
    };

    /// <summary>Nacita linky a tabule servera.</summary>
    /// <param name="program">program servera</param>
    /// <param name="installation">instalacia servera</param>
    public static Result<InissStationDto> Read(string program, Iniss2Installation installation)
    {
        ArgumentNullException.ThrowIfNull(installation);

        IReadOnlyList<CommLine> commLines;
        List<StationTable> tables;
        IReadOnlyList<string> grafikons;
        Iniss2Devices.Result devices;
        try
        {
            var groups = DirListXml.Read(installation.DataDir);
            commLines = CommLinesXml.Read(installation.DataDir);
            tables = ReadTables(installation.DataDir, groups);
            grafikons = groups.Select(group => group.Name).ToList();
            var stations = groups.SelectMany(group => group.Items).Select(item => item.Station).ToHashSet();
            devices = Iniss2Devices.Resolve(commLines, LightsXml.Read(installation.DataDir), AudioXml.Read(installation.DataDir), stations);
        }
        catch (Exception e) when (e is IOException or FormatException or UnauthorizedAccessException or XmlException)
        {
            return Result.Failure<InissStationDto>(InissStationErrors.Unreadable(e.Message));
        }

        var lines = commLines.Select((line, index) => ToLine(line, index + 1, tables.Where(t => t.Table.Line == line.Key).ToList(), devices.For(line.Key))).ToList();
        var known = commLines.Select(line => line.Key).ToHashSet(StringComparer.Ordinal);
        var unserved = tables.Where(t => t.Table.Line.Length > 0 && !known.Contains(t.Table.Line))
            .Select(t => ToTable(t, [], new InissProblemDto(InissProblemSeverity.Error, InissProblemCode.UnknownLine, [t.Table.Line])))
            .ToList();

        var xml = tables.Where(t => t.Table.SaveFile.Length > 0)
            .DistinctBy(t => t.Table.SaveFile, StringComparer.OrdinalIgnoreCase)
            .Select(t => new StationXmlTableDto(t.Table.Name.Length > 0 ? $"{t.Table.Name} - {t.Table.Key}" : t.Table.Key, Path.GetFileNameWithoutExtension(t.Table.SaveFile)))
            .ToList();

        return Result.Success(new InissStationDto(program, Path.GetFileNameWithoutExtension(program), false, grafikons, lines, unserved, installation.LogDir,
            Directory.Exists(installation.LogDir), xml)
        {
            Iniss2 = true,
            DeviceProblems = devices.Problems
        });
    }

    /// <summary>
    /// Nove adresy liniek pri presmerovani na simulator: linky, ktore simulator zobrazi a server sa na nich pripaja na adresu.
    /// </summary>
    /// <param name="station">nacitany server</param>
    public static Dictionary<string, string> RedirectAddresses(InissStationDto station)
    {
        ArgumentNullException.ThrowIfNull(station);

        return station.Lines.Where(line => line is { Active: true, Supported: true, CanRedirect: true })
            .ToDictionary(line => line.Section, line => string.Create(CultureInfo.InvariantCulture, $"{LocalHost}:{line.SimulatorPort}"), StringComparer.Ordinal);
    }

    private static List<StationTable> ReadTables(string dataDir, IEnumerable<DirListGroup> groups)
    {
        var tables = new List<StationTable>();

        foreach (var group in groups)
        {
            foreach (var item in group.Items)
            {
                var stationDir = Path.Combine(dataDir, group.Name, item.Directory);
                var catalog = TableCatalogFile.Read(stationDir).GroupBy(t => t.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
                var tracks = File.Exists(Path.Combine(stationDir, PositionsDefFile.FileName)) ? PositionsDefFile.Read(stationDir) : [];
                var station = StationPrefix().Replace(item.Directory, "");

                tables.AddRange(TablesFile.Read(stationDir).Select(table =>
                {
                    var deviceClass = DeviceClass(table, catalog);
                    return new StationTable(group.Name, table, deviceClass, ToFace(table, deviceClass, catalog, tracks, station));
                }));
            }
        }

        return tables;
    }

    // druh zariadenia podla prvej casti tabule, ktora ma predlohu so zariadenim; tabula pre web ho nema
    private static string DeviceClass(TableDefinition table, Dictionary<string, CatalogTable> catalog) =>
        table.Parts.Select(part => catalog.GetValueOrDefault(part.CatalogKey)).FirstOrDefault(entry => entry is { IsDevice: true })?.Class ?? "";

    /// <summary>
    /// Co o cele tabule vedia data stanice. Druh a stlpce su z prvej casti, ktora nie je informacny text. Pocet riadkov je sucet
    /// vsetkych casti, preto je zaznam jeden riadok. Informacny riadok Elektrocasu byva dlhsi nez tabula a na sirku nema vplyv,
    /// pohyblivy text ELEN je siroky ako jeho stlpec.
    /// </summary>
    private static StationFaceDto? ToFace(TableDefinition table, string deviceClass, Dictionary<string, CatalogTable> catalog, IReadOnlyList<TrackDefinition> tracks, string station)
    {
        var parts = table.Parts.Select(part => (Part: part, Catalog: catalog.GetValueOrDefault(part.CatalogKey)))
            .Where(item => item.Catalog is { IsDevice: true })
            .Select(item => (item.Part, Catalog: item.Catalog!))
            .ToList();
        var main = parts.FirstOrDefault(item => item.Part.Kind != TablePart.Info);
        if (main.Catalog is null)
            return null;

        var characters = CharacterClasses.Contains(deviceClass);
        var scale = characters ? CharWidth : 1;
        var shown = main.Catalog.Columns.Where(column => column.IsVisible).ToList();
        var textRows = shown.Where(column => column.IsTextLine).Select(column => column.Row).ToHashSet();
        var width = shown.Where(column => !characters || !textRows.Contains(column.Row)).Select(column => column.End * scale).DefaultIfEmpty().Max();

        // tabula len s textom (slepecky hlasic) ma sirku z predvolenej tabule, riadky z predlohy
        if (width == 0)
            width = BoardProfile.DefaultFor(deviceClass).Width;

        var columns = shown.Where(column => column.Section is not null && !textRows.Contains(column.Row))
            .Select(column => new StationFaceColumnDto(column.Row, column.Start * scale, column.End * scale, Align(column.Align), column.Section!.Id))
            .ToList();

        var under = parts.SelectMany(item => item.Part.Tracks)
            .Select(key => tracks.FirstOrDefault(track => track.Key == key))
            .OfType<TrackDefinition>()
            .ToList();

        return new StationFaceDto(Kind(main.Part.Kind), 1, parts.Sum(item => Math.Max(1, item.Part.Records) * item.Catalog.LinesPerRecord), width,
            BoardProfile.DefaultFor(deviceClass).RowHeight, columns,
            under.Select(track => track.TrackText).Where(text => text.Length > 0).Distinct(StringComparer.Ordinal).ToList(),
            under.Select(track => track.PlatformText).Where(text => text.Length > 0).Distinct(StringComparer.Ordinal).ToList(),
            station);
    }

    private static StationBoardKind Kind(string partKind) => partKind switch
    {
        "DEPARTURE" => StationBoardKind.Departures,
        "ARRIVAL" => StationBoardKind.Arrivals,
        "PLATFORM" => StationBoardKind.Platform,
        "SUBWAY" => StationBoardKind.Underpass,
        _ => StationBoardKind.Other
    };

    private static ContractAlign Align(string align) => align switch
    {
        "RIGHT" => ContractAlign.Right,
        "CENTER" => ContractAlign.Center,
        _ => ContractAlign.Left
    };

    private static InissLineDto ToLine(CommLine line, int number, List<StationTable> tables, IReadOnlyList<InissControllerDto> controllers)
    {
        var tableClass = ClassOf(line, tables);
        var manufacturers = Simulator.ManufacturersFor(tableClass);

        // jedna tabula na adrese, aj ked ju uvadza viac stanic
        var devices = tables.Where(t => t.Class.Length > 0).DistinctBy(t => t.Table.Address).ToList();
        var problems = new List<InissProblemDto>();

        // linka ustredne AK Signal nema tabule, ale svetla a audio linky
        var controllerLine = tableClass is AkSignalClass or AkSignalUhClass;
        if (controllerLine && controllers.Count == 0)
            problems.Add(new InissProblemDto(InissProblemSeverity.Info, InissProblemCode.NoDevices, []));
        else if (!controllerLine && devices.Count == 0)
            problems.Add(new InissProblemDto(InissProblemSeverity.Info, InissProblemCode.NoTables, []));

        foreach (var device in devices.Where(t => manufacturers.Count > 0 && !manufacturers.Contains(t.Class, StringComparer.OrdinalIgnoreCase)))
            problems.Add(new InissProblemDto(InissProblemSeverity.Warning, InissProblemCode.WrongFamily, [device.Table.Key, device.Class, tableClass.ToString(CultureInfo.InvariantCulture)]));

        return new InissLineDto(line.Key, number, tableClass, line.Address, SimulatorPort(line, number), !line.Omitted, manufacturers.Count > 0,
            devices.Select(device => ToTable(device, manufacturers, null)).ToList(), problems)
        {
            Controllers = controllerLine ? controllers : [],
            Transport = line.Type == CommLineType.UdpClient ? StationLineTransport.Udp : StationLineTransport.Tcp,
            CanRedirect = line.CanRedirect,
            OriginalPort = line.OriginalAddress
        };
    }

    private static int ClassOf(CommLine line, List<StationTable> tables)
    {
        if (ProtocolClasses.TryGetValue(line.Protocol, out var tableClass))
            return tableClass;

        if (line.Protocol != ElektrocasProtocol)
            return 0;

        if (tables.Exists(t => t.Class == "FERS"))
            return FersClass;

        return tables.Exists(t => t.Class is "LCD1" or "ERP") ? Lcd1Class : LcdClass;
    }

    private static InissTableDto ToTable(StationTable table, IReadOnlyList<string> manufacturers, InissProblemDto? problem)
    {
        // simulator ma vyrobcov v zapise INISS 3.39 (ELEN16Kam), INISS2 ich pise velkymi pismenami
        var manufacturer = manufacturers.FirstOrDefault(m => string.Equals(m, table.Class, StringComparison.OrdinalIgnoreCase));

        return new InissTableDto(table.Table.Key, table.Table.Address, table.Group, manufacturer ?? table.Class, false, manufacturer is not null, problem) { Face = table.Face };
    }

    /// <summary>Port linky v simulatore: z presmerovanej adresy, inak 47000 + cislo.</summary>
    private static int SimulatorPort(CommLine line, int number)
    {
        var colon = line.Address.LastIndexOf(':');

        return line.OriginalAddress is not null && colon >= 0 && int.TryParse(line.Address.AsSpan(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            ? port
            : LineEndpoint.DefaultFor(number).Port;
    }

    private sealed record StationTable(string Group, TableDefinition Table, string Class, StationFaceDto? Face);

    /// <summary>Poradove cislo pred nazvom stanice v priecinku (<c>01 Lipovec</c>).</summary>
    [GeneratedRegex(@"^\d+\s+", RegexOptions.CultureInvariant)]
    private static partial Regex StationPrefix();
}
