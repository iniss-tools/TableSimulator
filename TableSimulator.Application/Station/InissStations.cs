using System.Buffers;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Faces;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using ToolsCore.Iniss.Grafikon;
using ToolsCore.Iniss.Registry;
using ToolsCore.Iniss.Tables;

namespace TableSimulator.Application.Station;

/// <summary>
/// Linky a tabule instalacie INISSu z nastavenia simulatora (program, vetva registra, spustanie ako spravca) - rovnako
/// ako ich vidi GVDEditor: grafikony z DirList, tabule z TPhysic/TKatalog, linky zo sekcii Driver* (.INI a vo Windows
/// aj register) a priradenie tabul ako v INISSe.
/// </summary>
public static class InissStations
{
    // znaky, ktore meno suboru XML nesmie mat (rovnako ako overenie importu stanice)
    /// <summary>Vyrobca spinacej jednotky zosilnovaca v simulatore.</summary>
    private const string AmplifierManufacturer = "ELSVO";

    private static readonly SearchValues<char> InvalidXmlName = SearchValues.Create("\\/:*?\"<>|");

    /// <summary>Nacita instalaciu INISSu podla nastavenia.</summary>
    public static Result<InissStationDto> Read(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var program = settings.InissProgram.Trim();
        if (program.Length == 0)
            return Result.Failure<InissStationDto>(InissStationErrors.NotConfigured);
        if (!File.Exists(program))
            return Result.Failure<InissStationDto>(InissStationErrors.ProgramNotFound(program));
        var installationDir = Path.GetDirectoryName(Path.GetFullPath(program))!;
        var dataDir = Path.Combine(installationDir, GvdFileConsts.DirData);
        if (!Directory.Exists(dataDir))
            return Result.Failure<InissStationDto>(InissStationErrors.DataNotFound(dataDir));

        IReadOnlyList<DirList> dirs;
        try
        {
            dirs = DirListFile.Read(dataDir);
        }
        catch (Exception e) when (e is IOException or FormatException or UnauthorizedAccessException)
        {
            return Result.Failure<InissStationDto>(InissStationErrors.Unreadable(e.Message));
        }

        var tables = InissTableMap.Build(dirs);
        var appName = InissRegistryReader.AppNameFor(program, settings.InissRegistry);
        var mode = settings.InissElevated ? InissRunMode.Elevated : InissRunMode.Normal;
        var info = tables.ToDictionary(t => t.Index, t => t.ToInfo());
        var source = OperatingSystem.IsWindows()
            ? InissRegistryReader.LoadSource(appName, program, mode, info)
            : new InissConfigSource
            {
                AppName = appName, RunMode = mode, Ini = InissIniFile.Load(InissRegistryReader.IniPathFor(program)),
                Version = InissRegistryReader.ExeVersion(program), Tables = info
            };
        var resolved = RegResolver.Resolve(source);
        var map = DriverLines.Build(resolved, tables);
        var logFolder = InissPaths.LogDirectory(resolved, installationDir);

        // spinacie jednotky zosilnovacov nie su v TPhysic, ale v zvukovych okruhoch - linka podla cisla (E<n>)
        IReadOnlyList<ElsvoUnit> units;
        try
        {
            units = ElsvoUnits.Read(dataDir);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<InissStationDto>(InissStationErrors.Unreadable(e.Message));
        }

        var lines = map.Lines.Select(line =>
        {
            var manufacturers = Simulator.ManufacturersFor(line.Class);
            var amplifiers = units.Where(u => u.Line == line.Line)
                .Select(u => new InissAmplifierDto(u.Line, u.Address, u.Name, manufacturers.Contains(AmplifierManufacturer, StringComparer.OrdinalIgnoreCase)))
                .ToList();
            return new InissLineDto(line.Section, line.Line, line.Class, line.Port, SimulatorPort(line), line.Active, manufacturers.Count > 0,
                line.Tables.Select(t => Table(t.Table, t.Automatic, manufacturers, null)).ToList(), line.Problems.Select(Problem).ToList())
            {
                Amplifiers = amplifiers
            };
        }).ToList();
        var unservedAmplifiers = units.Where(u => !map.Lines.Any(l => l.Line == u.Line))
            .Select(u => new InissAmplifierDto(u.Line, u.Address, u.Name, false)).ToList();
        var unserved = map.Unserved.Select(u => Table(u.Table, u.Table.Table.CommunicationPort == 0, [], Problem(u.Problem))).ToList();

        // INISS zapisuje XML vsetkych grafikonov z DirList - rovnake SAVE_XML zapisuje do jedneho suboru
        var xml = tables.Where(t => !string.IsNullOrWhiteSpace(t.Table.SaveXML))
            .DistinctBy(t => t.Table.SaveXML.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(t => new StationXmlTableDto(t.Table.Key, t.Table.SaveXML.Trim()))
            .ToList();
        var grafikons = tables.Select(t => t.Grafikon).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var registry = OperatingSystem.IsWindows() && (source.Machine.Exists || source.User.Exists || source.VirtualStore.Exists);
        return Result.Success(new InissStationDto(program, appName, registry, grafikons, lines, unserved, logFolder, Directory.Exists(logFolder), xml)
        {
            UnservedAmplifiers = unservedAmplifiers
        });
    }

    /// <summary>
    /// Linky a tabule na zalozenie v simulatore: aktivne linky s cislom a ich tabule so znamym vyrobcom (s datami
    /// pre celo), priecinok logov a tabule s exportom do XML.
    /// </summary>
    public static StationImportRequest ToRequest(InissStationDto station, InissStationImportRequest? options = null)
    {
        ArgumentNullException.ThrowIfNull(station);
        var lines = new List<StationLineDto>();
        var boards = new List<StationBoardDto>();
        foreach (var line in station.Lines)
        {
            if (!line.Active || line.Number is not { } number)
                continue;
            lines.Add(new StationLineDto(number, line.TableClass, line.SimulatorPort, line.Section));
            boards.AddRange(line.Tables.Where(t => t.Manufacturer.Length > 0 && t.Address is >= 0 and <= 255)
                .Select(t => new StationBoardDto(number, t.Address, t.Manufacturer, t.Name) { Flaps = t.Flaps, FlapRecords = t.FlapRecords, Face = t.Face }));
            boards.AddRange(line.Amplifiers.Select(a => new StationBoardDto(number, a.Address, AmplifierManufacturer, a.Name)));
        }

        var xml = station.XmlTables.Where(t => t.Source.Length <= 100 && t.Source.AsSpan().IndexOfAny(InvalidXmlName) < 0).ToList();
        return new StationImportRequest(lines, boards)
        {
            LogFolder = station.LogFolder, XmlTables = xml, FaceLanguage = options?.FaceLanguage ?? StationFaceLanguage.Sk,
            FacesForExisting = options?.FacesForExisting ?? false
        };
    }

    /// <summary>Port linky v simulatore: z TablePort, ak INISS posiela cez TCP (napr. uz presmerovany), inak 47000 + cislo.</summary>
    private static int SimulatorPort(DriverLine line)
    {
        var port = line.Port.Trim();
        var equals = port.IndexOf('=', StringComparison.Ordinal);
        if (DriverDefaults.LineNumber(port) is not null && equals >= 0)
            port = port[(equals + 1)..];
        return LineEndpoint.TryParse(port) is { Transport: LineTransport.Tcp, Port: > 0 } endpoint
            ? endpoint.Port
            : LineEndpoint.DefaultFor(line.Line ?? 0).Port;
    }

    private static InissTableDto Table(InissTable table, bool automatic, IReadOnlyList<string> manufacturers, InissProblemDto? problem)
    {
        var catalog = table.Table.TableCatalog;
        var manufacturer = catalog?.Manufacturer?.Name ?? "";
        var dto = new InissTableDto(table.Table.Key, table.Table.ID, table.Grafikon, manufacturer, automatic,
            manufacturers.Contains(manufacturer, StringComparer.OrdinalIgnoreCase), problem) { Face = Face(table.Face) };
        if (catalog is null || !FlapLayouts.IsFlapBoard(catalog.Manufacturer))
            return dto;

        // listova tabula: co je na listoch, vie len predloha a jej prekodovacie tabulky
        var layout = FlapLayouts.Build(catalog);
        return dto with { Flaps = Flaps(layout), FlapRecords = layout.Records };
    }

    /// <summary>Co o cele tabule vedia data stanice (rovnako posiela GVDEditor).</summary>
    public static StationFaceDto? Face(TableFaceInfo? info) => info is null
        ? null
        : new StationFaceDto(Kind(info.ViewType), info.LinesPerRecord, info.Records, info.Width, info.RowHeight,
            info.Columns.Select(c => new StationFaceColumnDto(c.Line, c.Start, c.End, c.Align switch
            {
                1 => FaceAlign.Right,
                2 => FaceAlign.Center,
                _ => FaceAlign.Left
            }, c.Content)).ToList(),
            info.Tracks, info.Platforms, info.Station);

    private static StationBoardKind Kind(string viewType)
    {
        var type = TableViewType.Parse(viewType);
        return type == TableViewType.Odchodova ? StationBoardKind.Departures
            : type == TableViewType.Prichodova ? StationBoardKind.Arrivals
            : type == TableViewType.Nastupistna ? StationBoardKind.Platform
            : type == TableViewType.Podchodova ? StationBoardKind.Underpass
            : StationBoardKind.Other;
    }

    private static FlapProfileDto Flaps(FlapLayout layout) => new()
    {
        LinesPerRecord = layout.LinesPerRecord,
        Modules = layout.Modules.Select(m => new FlapModuleDto(m.Line, m.Position, m.Span, m.List)).ToArray(),
        Lists = layout.Lists
    };

    private static InissProblemDto Problem(DriverLineProblem problem) => new(
        problem.Severity switch
        {
            RegSeverity.Error => InissProblemSeverity.Error,
            RegSeverity.Warning => InissProblemSeverity.Warning,
            _ => InissProblemSeverity.Info
        },
        problem.Code switch
        {
            DriverLineProblemCode.LineOutOfRange => InissProblemCode.LineOutOfRange,
            DriverLineProblemCode.LineDuplicate => InissProblemCode.LineDuplicate,
            DriverLineProblemCode.WrongFamily => InissProblemCode.WrongFamily,
            DriverLineProblemCode.NoTables => InissProblemCode.NoTables,
            DriverLineProblemCode.NoAutomaticLine => InissProblemCode.NoAutomaticLine,
            _ => InissProblemCode.NoDriver
        },
        problem.Args);
}
