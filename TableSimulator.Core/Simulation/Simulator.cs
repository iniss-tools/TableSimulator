using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Protocols.Elen;

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
    private long _seq;

    public Simulator(TimeProvider? time = null)
    {
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Udalosti pre rozhranie; vyvolavaju sa mimo zamku.</summary>
    public event Action<SimulatorEvent>? Changed;

    /// <summary>Triedy liniek (TableClass), ktore simulator pozna.</summary>
    public static IReadOnlyList<int> SupportedClasses => [.. Protocols.Keys];

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
                    Name = l.Name, Mode = l.Mode, AutoLearn = l.AutoLearn, Manufacturer = l.Manufacturer
                }).ToList(),
                Boards = Boards.Select(b => new BoardConfig(b.Line, b.Address, b.ManufacturerKey)
                {
                    Name = b.Name, Profile = b.Profile, Design = b.Design, Fault = b.Fault
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

    /// <summary>Zmeni stav spojenia linky (volá prenos).</summary>
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

            line.Status = status;
            line.StatusDetail = detail;
            line.Version++;
        }

        Raise([new LineChanged(number)]);
    }

    /// <summary>
    /// Spracuje bajty prijate na linke a vrati odpovede, ktore treba INISSu poslat.
    /// </summary>
    public IReadOnlyList<LineReply> Receive(int lineNumber, ReadOnlySpan<byte> data)
    {
        Context context;
        lock (_lock)
        {
            if (!_lines.TryGetValue(lineNumber, out var line))
                return [];

            line.Stats.BytesIn += data.Length;
            context = new Context(this, line);
            line.Protocol.Receive(data, context);
            foreach (var reply in context.Replies)
                line.Stats.BytesOut += reply.Bytes.Length;
            line.Version++;
        }

        context.Events.Add(new LineChanged(lineNumber));
        Raise(context.Events);
        return context.Replies;
    }

    private SimLine AddLineCore(LineConfig config)
    {
        if (config.Number is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(config), config.Number, "Line number must be 1-100.");

        var protocol = CreateProtocol(config.TableClass)
                       ?? throw new ArgumentException(string.Create(CultureInfo.InvariantCulture, $"Unsupported TableClass {config.TableClass}."), nameof(config));
        var endpoint = LineEndpoint.TryParse(config.Endpoint)
                       ?? throw new ArgumentException($"Invalid endpoint '{config.Endpoint}'.", nameof(config));

        var old = _lines.GetValueOrDefault(config.Number);
        var line = new SimLine(config.Number, config.TableClass, protocol)
        {
            Stats = old?.Stats ?? new LineStats(),
            Name = config.Name,
            Endpoint = endpoint,
            Mode = config.Mode,
            AutoLearn = config.AutoLearn,
            Manufacturer = config.Manufacturer
        };

        if (old != null)
        {
            line.Status = old.Status;
            line.StatusDetail = old.StatusDetail;
        }

        _lines[config.Number] = line;
        return line;
    }

    private SimBoard AddBoardCore(BoardConfig config)
    {
        if (!_lines.TryGetValue(config.Line, out var line))
            throw new ArgumentException(string.Create(CultureInfo.InvariantCulture, $"Line {config.Line} does not exist."), nameof(config));

        var id = SimBoard.MakeId(config.Line, config.Address);
        var display = _boards.TryGetValue(id, out var old) ? old.Display : line.Protocol.CreateDisplay();
        var board = new SimBoard(config.Line, config.Address, config.Name, config.Manufacturer, config.Profile, display)
        {
            Design = config.Design,
            Fault = config.Fault,
            AutoCreated = old?.AutoCreated ?? false,
            Stats = old?.Stats ?? new BoardStats(),
            Version = (old?.Version ?? 0) + 1
        };
        _boards[id] = board;
        return board;
    }

    // protokoly podla triedy linky (TableClass)
    private static readonly Dictionary<int, Func<ILineProtocol>> Protocols = new()
    {
        [4] = () => new ElenProtocol()
    };

    private static ILineProtocol? CreateProtocol(int tableClass) => Protocols.GetValueOrDefault(tableClass)?.Invoke();

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

        public SimLine Line => line;

        public DateTimeOffset Now => sim.Now;

        public IReadOnlyList<SimBoard> Boards => sim._boards.Values.Where(b => b.Line == line.Number).ToList();

        public SimBoard? FindBoard(int address)
        {
            var id = SimBoard.MakeId(line.Number, address);
            if (sim._boards.TryGetValue(id, out var board))
                return board;
            if (!line.AutoLearn)
                return null;

            board = new SimBoard(line.Number, address, "", line.Manufacturer,
                BoardProfile.DefaultFor(line.Manufacturer) with { AutoSize = true }, line.Protocol.CreateDisplay())
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

        public void Reply(SimBoard board, byte[] bytes, FrameInfo info)
        {
            if (line.Mode == LineMode.Monitor)
                return;

            var status = FrameStatus.Ok;
            var delay = TimeSpan.Zero;
            switch (board.Fault.Kind)
            {
                case FaultKind.NoReply:
                    board.Stats.Unanswered++;
                    Events.Add(new FrameLogged(sim.AddLog(line.Number, FrameDirection.Sent, board.Address, [], FrameStatus.Fault, new FrameInfo("fault.noReply"))));
                    return;
                case FaultKind.Nak:
                    bytes = [ElenFrame.Nak];
                    info = new FrameInfo("nak");
                    status = FrameStatus.Fault;
                    break;
                case FaultKind.Garbage:
                    bytes = [0x00];
                    info = new FrameInfo("garbage");
                    status = FrameStatus.Fault;
                    break;
                case FaultKind.Delay:
                    delay = TimeSpan.FromMilliseconds(Math.Max(0, board.Fault.DelayMs));
                    status = FrameStatus.Fault;
                    break;
            }

            if (bytes is [ElenFrame.Ack])
                board.Stats.Acks++;
            else if (bytes is [ElenFrame.Nak])
                board.Stats.Naks++;

            Replies.Add(new LineReply(bytes, delay));
            Events.Add(new FrameLogged(sim.AddLog(line.Number, FrameDirection.Sent, board.Address, bytes, status, info)));
        }
    }
}
