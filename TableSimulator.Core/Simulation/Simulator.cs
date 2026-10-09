using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Protocols.Apel;
using TableSimulator.Core.Protocols.Elsvo;
using TableSimulator.Core.Protocols.Elektrocas;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Protocols.Ers;
using TableSimulator.Core.Protocols.Remote;
using TableSimulator.Core.Recording;

namespace TableSimulator.Core.Simulation;

/// <summary>
/// Udalost simulatora pre rozhranie.
/// </summary>
public abstract record SimulatorEvent;

/// <summary>Tabula zmenila obsah alebo nastavenie.</summary>
public sealed record BoardChanged(string BoardId) : SimulatorEvent;

/// <summary>Linka zmenila stav alebo nastavenie.</summary>
public sealed record LineChanged(int Line) : SimulatorEvent;

/// <summary>Pribudli alebo ubudli linky ci tabule.</summary>
public sealed record StructureChanged : SimulatorEvent;

/// <summary>Novy zaznam komunikacie.</summary>
public sealed record FrameLogged(FrameRecord Record) : SimulatorEvent;

/// <summary>
/// Odkial prisli bajty linky.
/// </summary>
public enum TrafficSource
{
    /// <summary>Od INISSu po linke.</summary>
    Line,

    /// <summary>Z prehravaneho zaznamu - nezapisuje sa do prebiehajuceho zaznamu.</summary>
    Replay
}

/// <summary>
/// Odpoved INISSu - posle sa po <see cref="Delay" />.
/// </summary>
public sealed record LineReply(byte[] Bytes, TimeSpan Delay);

/// <summary>
/// Simulator tabul: linky, tabule na nich a zaznam komunikacie. Bez sietoveho prenosu - ten zabezpecuje
/// <see cref="Transport.LineHost" />, ktory sem posiela prijate bajty a odosiela vratene odpovede.
/// Vsetky metody su bezpecne pre volanie z viacerych vlakien.
/// </summary>
public sealed class Simulator
{
    /// <summary>Kolko zaznamov komunikacie sa drzi.</summary>
    public const int LogCapacity = 5000;

    private readonly Lock _lock = new();
    private readonly SortedDictionary<int, SimLine> _lines = [];
    private readonly Dictionary<string, SimBoard> _boards = [];
    private readonly LinkedList<FrameRecord> _log = [];
    private readonly TimeProvider _time;
    private SessionRecorder? _recorder;
    private long _seq;

    public Simulator(TimeProvider? time = null)
    {
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Udalosti pre rozhranie; vyvolavaju sa mimo zamku.</summary>
    public event Action<SimulatorEvent>? Changed;

    /// <summary>Prijate bajty linky (od INISSu aj zo zaznamu) - pre zrkadlo; vyvolava sa mimo zamku v poradi prijatia.</summary>
    public event Action<int, byte[]>? Traffic;

    /// <summary>Triedy liniek (TableClass), ktore simulator pozna.</summary>
    public static IReadOnlyList<int> SupportedClasses => [.. Protocols.Keys.Order()];

    /// <summary>Vyrobcovia tabul (<c>MANUFACTURER_KEY</c>), ktore simulator na linke danej triedy zobrazi; prvy je predvoleny.</summary>
    public static IReadOnlyList<string> ManufacturersFor(int tableClass) =>
        Protocols.TryGetValue(tableClass, out var p) ? p.Manufacturers : [];

    /// <summary>Aktualny cas simulatora.</summary>
    public DateTimeOffset Now => _time.GetUtcNow();

    /// <summary>Vykona <paramref name="action" /> pod zamkom simulatora - na citanie konzistentneho stavu.</summary>
    public T Read<T>(Func<Simulator, T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (_lock)
            return action(this);
    }

    /// <summary>Linky podla cisla (citat pod <see cref="Read{T}" />).</summary>
    public IEnumerable<SimLine> Lines => _lines.Values;

    /// <summary>Tabule podla linky a adresy (citat pod <see cref="Read{T}" />).</summary>
    public IEnumerable<SimBoard> Boards => _boards.Values.OrderBy(b => b.Line).ThenBy(b => b.Address);

    /// <summary>Linka podla cisla (citat pod <see cref="Read{T}" />).</summary>
    public SimLine? FindLine(int number) => _lines.GetValueOrDefault(number);

    /// <summary>Tabula podla identifikatora (citat pod <see cref="Read{T}" />).</summary>
    public SimBoard? FindBoard(string id) => _boards.GetValueOrDefault(id);

    /// <summary>Zaznamy komunikacie s poradovym cislom vacsim ako <paramref name="afterSeq" />.</summary>
    public IReadOnlyList<FrameRecord> LogSince(long afterSeq, int max = LogCapacity)
    {
        lock (_lock)
            return _log.Where(r => r.Seq > afterSeq).TakeLast(max).ToList();
    }

    /// <summary>Vymaze zaznam komunikacie.</summary>
    public void ClearLog()
    {
        lock (_lock)
            _log.Clear();
    }

    /// <summary>Nahradi vsetky linky a tabule podla nastavenia.</summary>
    public void Load(SimulatorConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        lock (_lock)
        {
            _lines.Clear();
            _boards.Clear();
            foreach (var line in config.Lines)
                AddLineCore(line);
            foreach (var board in config.Boards)
                AddBoardCore(board);
        }

        Raise([new StructureChanged()]);
    }

    /// <summary>Aktualne nastavenie na ulozenie.</summary>
    public SimulatorConfig ToConfig()
    {
        lock (_lock)
        {
            return new SimulatorConfig
            {
                Lines = _lines.Values.Select(l => new LineConfig(l.Number, l.TableClass, l.Endpoint.ToString())
                {
                    Name = l.Name, Mode = l.Mode, AutoLearn = l.AutoLearn, Manufacturer = l.Manufacturer, Mirror = l.Mirror?.ToString()
                }).ToList(),
                Boards = Boards.Select(b => new BoardConfig(b.Line, b.Address, b.ManufacturerKey)
                {
                    Name = b.Name, Profile = b.Profile, Design = b.Design, Fault = b.Fault, SecondAddress = b.SecondAddress
                }).ToList()
            };
        }
    }

    /// <summary>Prida linku alebo nahradi linku s rovnakym cislom (tabule na nej ostanu).</summary>
    public SimLine SetLine(LineConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        SimLine line;
        lock (_lock)
            line = AddLineCore(config);
        Raise([new StructureChanged()]);
        return line;
    }

    /// <summary>
    /// Zalozi linky a tabule stanice: chybajuce linky prida (samoucenie zapnute), existujucim zmeni triedu a port
    /// (nazov, rezim a zrkadlo ostanu), chybajuce tabule prida s profilom podla vyrobcu, ktory sa zvacsi podla obsahu;
    /// tabule, ktore uz su, necha tak, ako ich obsluha nastavila.
    /// </summary>
    public StationImportResult ImportStation(StationImport import)
    {
        ArgumentNullException.ThrowIfNull(import);
        int linesAdded = 0, linesUpdated = 0, boardsAdded = 0, boardsKept = 0;
        var unsupported = new List<int>();
        var skipped = new List<string>();
        lock (_lock)
        {
            foreach (var station in import.Lines)
            {
                var manufacturers = ManufacturersFor(station.TableClass);
                if (manufacturers.Count == 0 || station.Number is < 1 or > 100)
                {
                    unsupported.Add(station.Number);
                    continue;
                }

                var endpoint = new LineEndpoint(LineTransport.Tcp, "0.0.0.0", station.Port);
                // vyrobca samoucenia podla prvej tabule linky, ktoru simulator zobrazi
                var manufacturer = import.Boards.Where(b => b.Line == station.Number).Select(b => b.Manufacturer)
                    .FirstOrDefault(m => manufacturers.Contains(m, StringComparer.OrdinalIgnoreCase)) ?? manufacturers[0];
                if (_lines.TryGetValue(station.Number, out var old))
                {
                    // linka uz pocuva - pri TCP ostane jej adresa, zmeni sa len port
                    if (old.Endpoint.Transport == LineTransport.Tcp)
                        endpoint = old.Endpoint with { Port = station.Port };
                    if (old.TableClass == station.TableClass && old.Endpoint == endpoint)
                        continue;
                    AddLineCore(new LineConfig(old.Number, station.TableClass, endpoint.ToString())
                    {
                        Name = old.Name.Length > 0 ? old.Name : station.Name,
                        Mode = old.Mode,
                        AutoLearn = old.AutoLearn,
                        Manufacturer = manufacturers.Contains(old.Manufacturer, StringComparer.OrdinalIgnoreCase) ? old.Manufacturer : manufacturer,
                        Mirror = old.Mirror?.ToString()
                    });
                    linesUpdated++;
                }
                else
                {
                    AddLineCore(new LineConfig(station.Number, station.TableClass, endpoint.ToString())
                    {
                        Name = station.Name, AutoLearn = true, Manufacturer = manufacturer
                    });
                    linesAdded++;
                }
            }

            foreach (var board in import.Boards)
            {
                if (unsupported.Contains(board.Line) || !_lines.TryGetValue(board.Line, out var line))
                    continue;
                var key = ManufacturersFor(line.TableClass).FirstOrDefault(m => string.Equals(m, board.Manufacturer, StringComparison.OrdinalIgnoreCase));
                if (key is null)
                {
                    skipped.Add(SimBoard.MakeId(board.Line, board.Address));
                    continue;
                }

                if (_boards.TryGetValue(SimBoard.MakeId(board.Line, board.Address), out var existing))
                {
                    // zoznamy listov su data stanice, nie nastavenie obsluhy - prevezmu sa aj pre tabulu, ktora uz je
                    if (board.Flaps is { } flaps && existing.Display is FlapDisplay)
                    {
                        // farby listov nastavene v simulatore ostanu
                        existing.Profile = existing.Profile with { Flaps = flaps with { Colors = FlapColors.Keep(existing.Profile.Flaps, flaps) } };
                        existing.Version++;
                    }

                    boardsKept++;
                    continue;
                }

                var profile = BoardProfile.DefaultFor(key) with { AutoSize = true };
                if (board.Flaps is { } layout)
                {
                    var positions = layout.Modules.Count == 0 ? profile.Width / 8 : layout.Modules.Max(m => m.Position + m.Span);
                    profile = profile with
                    {
                        Flaps = layout, Rows = Math.Max(1, board.FlapRecords * Math.Max(1, layout.LinesPerRecord)), Width = Math.Max(1, positions) * 8
                    };
                }

                AddBoardCore(new BoardConfig(board.Line, board.Address, key) { Name = board.Name, Profile = profile });
                boardsAdded++;
            }
        }

        Raise([new StructureChanged()]);
        return new StationImportResult(linesAdded, linesUpdated, boardsAdded, boardsKept, unsupported, skipped);
    }

    /// <summary>Odstrani linku aj s jej tabulami.</summary>
    public bool RemoveLine(int number)
    {
        lock (_lock)
        {
            if (!_lines.Remove(number))
                return false;
            foreach (var id in _boards.Values.Where(b => b.Line == number).Select(b => b.Id).ToList())
                _boards.Remove(id);
        }

        Raise([new StructureChanged()]);
        return true;
    }

    /// <summary>Prida tabulu alebo nahradi nastavenie tabule s rovnakou linkou a adresou.</summary>
    public SimBoard SetBoard(BoardConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        SimBoard board;
        lock (_lock)
            board = AddBoardCore(config);
        Raise([new StructureChanged()]);
        return board;
    }

    /// <summary>Odstrani tabulu.</summary>
    public bool RemoveBoard(string id)
    {
        lock (_lock)
        {
            if (!_boards.Remove(id))
                return false;
        }

        Raise([new StructureChanged()]);
        return true;
    }

    /// <summary>Nastavi predstieranu poruchu tabule.</summary>
    public bool SetFault(string id, BoardFault fault)
    {
        lock (_lock)
        {
            if (!_boards.TryGetValue(id, out var board))
                return false;
            board.Fault = fault;
            board.Version++;
        }

        Raise([new BoardChanged(id)]);
        return true;
    }

    /// <summary>Zmaze obsah tabule (ako po zapnuti).</summary>
    public bool ClearBoard(string id)
    {
        lock (_lock)
        {
            if (!_boards.TryGetValue(id, out var board))
                return false;
            board.Display.Clear();
            board.Version++;
        }

        Raise([new BoardChanged(id)]);
        return true;
    }

    /// <summary>
    /// Zmeni stav spojenia linky (vola prenos). Cakanie po pripojenom INISSe je <see cref="LineStatus.Lost" /> - INISS
    /// sa odpojil; po zastaveni linky (zmena koncoveho bodu) linka zase len caka.
    /// </summary>
    public void SetLineStatus(int number, LineStatus status, string? detail)
    {
        lock (_lock)
        {
            if (!_lines.TryGetValue(number, out var line))
                return;
            if (status == LineStatus.Connected && line.Status != LineStatus.Connected)
            {
                line.Stats.Connections++;
                line.Protocol.Reset();
            }

            if (status == LineStatus.Listening && line.Status is LineStatus.Connected or LineStatus.Lost)
                status = LineStatus.Lost;
            if (status != line.Status)
                line.StatusSince = Now;
            line.Status = status;
            line.StatusDetail = detail;
            line.Version++;
        }

        Raise([new LineChanged(number)]);
    }

    /// <summary>
    /// Spracuje bajty prijate na linke a vrati odpovede, ktore treba INISSu poslat.
    /// </summary>
    /// <param name="lineNumber">cislo linky</param>
    /// <param name="data">prijate bajty</param>
    /// <param name="source">od INISSu alebo zo zaznamu</param>
    public IReadOnlyList<LineReply> Receive(int lineNumber, ReadOnlySpan<byte> data, TrafficSource source = TrafficSource.Line)
    {
        Context context;
        lock (_lock)
        {
            if (!_lines.TryGetValue(lineNumber, out var line))
                return [];

            line.Stats.BytesIn += data.Length;
            if (source == TrafficSource.Line)
                line.LastReceivedAt = Now;
            context = new Context(this, line);
            line.Protocol.Receive(data, context);
            foreach (var reply in context.Replies)
                line.Stats.BytesOut += reply.Bytes.Length;
            line.Version++;

            if (source == TrafficSource.Line && _recorder is { } recorder)
            {
                var now = Now;
                recorder.Add(lineNumber, FrameDirection.Received, data, now);
                foreach (var reply in context.Replies)
                    recorder.Add(lineNumber, FrameDirection.Sent, reply.Bytes, now);
            }
        }

        context.Events.Add(new LineChanged(lineNumber));
        Raise(context.Events);
        if (context.Forwarded is { } forwarded)
        {
            foreach (var frame in forwarded)
                Traffic?.Invoke(lineNumber, frame);
        }
        else
        {
            Traffic?.Invoke(lineNumber, data.ToArray());
        }

        return context.Replies;
    }

    /// <summary>Zapise chybu zrkadla linky (null = v poriadku).</summary>
    public void SetMirrorError(int number, string? error)
    {
        lock (_lock)
        {
            if (!_lines.TryGetValue(number, out var line) || line.MirrorError == error)
                return;
            line.MirrorError = error;
            line.Version++;
        }

        Raise([new LineChanged(number)]);
    }

    /// <summary>Zacne zapisovat prevadzku liniek do zaznamu; predchadzajuci zaznam vrati (treba ho ukoncit).</summary>
    public SessionRecorder? StartRecording(SessionRecorder recorder)
    {
        lock (_lock)
        {
            var previous = _recorder;
            _recorder = recorder;
            return previous;
        }
    }

    /// <summary>Prestane zapisovat do zaznamu a vrati ho (treba ho ukoncit); null = zaznam nebezal.</summary>
    public SessionRecorder? StopRecording()
    {
        lock (_lock)
        {
            var recorder = _recorder;
            _recorder = null;
            return recorder;
        }
    }

    /// <summary>Zahodi rozpracovanu spravu linky (zaciatok prehravania, nove spojenie).</summary>
    public void ResetLine(int number)
    {
        lock (_lock)
            _lines.GetValueOrDefault(number)?.Protocol.Reset();
    }

    /// <summary>
    /// Doplni linky a tabule, ktore v simulatore chybaju (napr. zo zaznamu z ineho pocitaca); existujuce nemeni.
    /// </summary>
    /// <returns>pribudla aspon jedna linka (treba spustit jej server)</returns>
    public bool AddMissing(SimulatorConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var linesAdded = false;
        var changed = false;
        lock (_lock)
        {
            foreach (var line in config.Lines.Where(l => !_lines.ContainsKey(l.Number)))
            {
                try
                {
                    AddLineCore(line);
                    linesAdded = changed = true;
                }
                catch (ArgumentException)
                {
                    // trieda alebo koncovy bod, ktore tento simulator nepozna
                }
            }

            foreach (var board in config.Boards.Where(b => _lines.ContainsKey(b.Line) && !_boards.ContainsKey(SimBoard.MakeId(b.Line, b.Address))))
            {
                AddBoardCore(board);
                changed = true;
            }
        }

        if (changed)
            Raise([new StructureChanged()]);
        return linesAdded;
    }

    private SimLine AddLineCore(LineConfig config)
    {
        if (config.Number is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(config), config.Number, "Line number must be 1-100.");

        var protocol = CreateProtocol(config.TableClass)
                       ?? throw new ArgumentException(string.Create(CultureInfo.InvariantCulture, $"Unsupported TableClass {config.TableClass}."), nameof(config));
        var endpoint = LineEndpoint.TryParse(config.Endpoint)
                       ?? throw new ArgumentException($"Invalid endpoint '{config.Endpoint}'.", nameof(config));
        LineEndpoint? mirror = null;
        if (!string.IsNullOrWhiteSpace(config.Mirror))
        {
            mirror = LineEndpoint.TryParse(config.Mirror);
            if (mirror is not { Transport: LineTransport.Serial })
                throw new ArgumentException($"Invalid mirror '{config.Mirror}' - expected COMn.", nameof(config));
        }

        var old = _lines.GetValueOrDefault(config.Number);
        var line = new SimLine(config.Number, config.TableClass, protocol)
        {
            Stats = old?.Stats ?? new LineStats(),
            Name = config.Name,
            Endpoint = endpoint,
            Mode = config.Mode,
            AutoLearn = config.AutoLearn,
            // vyrobca samoucenia musi patrit k triede linky
            Manufacturer = ManufacturersFor(config.TableClass).FirstOrDefault(m => string.Equals(m, config.Manufacturer, StringComparison.OrdinalIgnoreCase))
                           ?? ManufacturersFor(config.TableClass)[0],
            Mirror = mirror
        };

        if (old != null)
        {
            line.Status = old.Status;
            line.StatusDetail = old.StatusDetail;
            line.StatusSince = old.StatusSince;
            line.LastReceivedAt = old.LastReceivedAt;
        }
        else
        {
            line.StatusSince = Now;
        }

        _lines[config.Number] = line;

        // ina rodina protokolu (napr. ELEN → LCD1) - tabule linky dostanu obsah novej rodiny
        foreach (var board in _boards.Values.Where(b => b.Line == line.Number).ToList())
        {
            var display = protocol.CreateDisplay(board.ManufacturerKey);
            if (board.Display.GetType() == display.GetType())
                continue;
            _boards[board.Id] = new SimBoard(board.Line, board.Address, board.Name, board.ManufacturerKey, board.Profile, display)
            {
                SecondAddress = board.SecondAddress,
                Design = board.Design,
                Fault = board.Fault,
                AutoCreated = board.AutoCreated,
                Stats = board.Stats,
                Version = board.Version + 1
            };
        }

        return line;
    }

    private SimBoard AddBoardCore(BoardConfig config)
    {
        if (!_lines.TryGetValue(config.Line, out var line))
            throw new ArgumentException(string.Create(CultureInfo.InvariantCulture, $"Line {config.Line} does not exist."), nameof(config));

        var id = SimBoard.MakeId(config.Line, config.Address);
        // obsah ostane, ak vyrobca patri do tej istej rodiny (na linke vzdialeneho INISSu mozu byt rozne)
        var display = line.Protocol.CreateDisplay(config.Manufacturer);
        if (_boards.TryGetValue(id, out var old) && old.Display.GetType() == display.GetType())
            display = old.Display;
        var board = new SimBoard(config.Line, config.Address, config.Name, config.Manufacturer, config.Profile, display)
        {
            SecondAddress = config.SecondAddress is > 0 and < 128 && config.SecondAddress != config.Address ? config.SecondAddress : null,
            Design = config.Design,
            Fault = config.Fault,
            AutoCreated = old?.AutoCreated ?? false,
            Stats = old?.Stats ?? new BoardStats(),
            // pri inom vyrobcovi zistenie neplati - overi sa na dalsich spravach
            Mismatch = string.Equals(old?.ManufacturerKey, config.Manufacturer, StringComparison.OrdinalIgnoreCase) ? old?.Mismatch : null,
            Version = (old?.Version ?? 0) + 1
        };
        _boards[id] = board;
        return board;
    }

    // protokoly podla triedy linky (TableClass) a vyrobcovia tabul, ktori na takej linke byvaju
    private static readonly Dictionary<int, (Func<ILineProtocol> Create, string[] Manufacturers)> Protocols = new()
    {
        [1] = (() => new ElektrocasProtocol(), ["LCD"]),
        [2] = (() => new ErsProtocol(), ["ERS"]),
        [10] = (() => new ApelProtocol(), ["APEL", "APEL_AN"]),
        [3] = (() => new ElektrocasProtocol(), ["FERS"]),
        [4] = (() => new ElenProtocol(), ["ELEN", "ELENOLD", "ELEN10", "ELEN16", "ELEN16Kam", "ELEKON"]),
        [5] = (() => new ElektrocasProtocol(), ["LCD1", "ERP"]),
        [128] = (() => new RemoteProtocol(), [.. RemoteProtocol.Manufacturers]),
        [129] = (() => new ElsvoProtocol(), ["ELSVO"]),
        [130] = (() => new ItpProtocol(), ["LCD1", "ERP", "LCD", "FERS"])
    };

    private static ILineProtocol? CreateProtocol(int tableClass) => Protocols.TryGetValue(tableClass, out var p) ? p.Create() : null;

    private void Raise(IEnumerable<SimulatorEvent> events)
    {
        var handler = Changed;
        if (handler == null)
            return;
        foreach (var e in events.Distinct())
            handler(e);
    }

    private FrameRecord AddLog(int line, FrameDirection direction, int? address, byte[] bytes, FrameStatus status, FrameInfo info)
    {
        var record = new FrameRecord(++_seq, Now, line, direction, address, bytes, status, info);
        _log.AddLast(record);
        while (_log.Count > LogCapacity)
            _log.RemoveFirst();
        return record;
    }

    /// <summary>
    /// Kontext spracovania prijatych bajtov - bezi pod zamkom simulatora.
    /// </summary>
    private sealed class Context(Simulator sim, SimLine line) : ILineContext
    {
        public List<SimulatorEvent> Events { get; } = [];

        public List<LineReply> Replies { get; } = [];

        /// <summary>Spravy tabul pre zrkadlo; null = zrkadli sa, co prislo (<see cref="ILineProtocol.MirrorsInnerFrames" />).</summary>
        public List<byte[]>? Forwarded { get; } = line.Protocol.MirrorsInnerFrames ? [] : null;

        public SimLine Line => line;

        public DateTimeOffset Now => sim.Now;

        public IReadOnlyList<SimBoard> Boards => sim._boards.Values.Where(b => b.Line == line.Number).ToList();

        public SimBoard? FindBoard(int address, string? manufacturer = null)
        {
            var id = SimBoard.MakeId(line.Number, address);
            if (sim._boards.TryGetValue(id, out var board))
                return board;
            if (sim._boards.Values.FirstOrDefault(b => b.Line == line.Number && b.SecondAddress == address) is { } second)
                return second;
            if (!line.AutoLearn)
                return null;

            manufacturer ??= line.Manufacturer;
            board = new SimBoard(line.Number, address, "", manufacturer,
                BoardProfile.DefaultFor(manufacturer) with { AutoSize = true }, line.Protocol.CreateDisplay(manufacturer))
            {
                AutoCreated = true
            };
            sim._boards[id] = board;
            Events.Add(new StructureChanged());
            return board;
        }

        public void Changed(SimBoard board)
        {
            board.Version++;
            Events.Add(new BoardChanged(board.Id));
        }

        public void LogReceived(byte[] bytes, int? address, FrameStatus status, FrameInfo info) =>
            Events.Add(new FrameLogged(sim.AddLog(line.Number, FrameDirection.Received, address, bytes, status, info)));

        public void Reply(SimBoard board, byte[] bytes, FrameInfo info, bool positive = true)
        {
            if (line.Mode == LineMode.Monitor)
                return;

            var reply = BoardFaults.Apply(board, line.Protocol, bytes, info, positive);
            if (reply.Bytes == null)
                Events.Add(new FrameLogged(sim.AddLog(line.Number, FrameDirection.Sent, board.Address, [], reply.Status, reply.Info)));
            else
                ReplyRaw(board.Address, reply.Bytes, reply.Delay, reply.Status, reply.Info);
        }

        public void ReplyRaw(int? address, byte[] bytes, TimeSpan delay, FrameStatus status, FrameInfo info, bool log = true)
        {
            if (line.Mode == LineMode.Monitor)
                return;

            Replies.Add(new LineReply(bytes, delay));
            if (log)
                Events.Add(new FrameLogged(sim.AddLog(line.Number, FrameDirection.Sent, address, bytes, status, info)));
        }

        public void Forward(byte[] frame) => Forwarded?.Add(frame);
    }
}
