using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols.Elektrocas;
using TableSimulator.Core.Protocols.Elen;
using TableSimulator.Core.Protocols.Ers;

namespace TableSimulator.Core.Protocols.Remote;

/// <summary>
/// Linka vzdialeneho INISSu (TableClass 128): simulator hra INISS spusteny s <c>/Remote</c> aj tabule pripojene k nemu.
/// Riadiaci INISS posiela spravy <c>S</c> s frontami hlaseni (<c>QNAM</c>, <c>RCMD</c>, <c>RWAV</c>…) a najviac jednym
/// prikazom pre tabulu (<c>TCLS</c> kod vyrobcu, <c>TADR</c> adresa, <c>TCMD</c> sprava protokolu tabule bez
/// kontrolneho suctu). Simulator spravu tabule dokonci, spracuje ako <see cref="ElenProtocol" /> alebo
/// <see cref="ElektrocasProtocol" /> a hned odpovie spravou <c>0xAC</c> s tym istym poradovym cislom: stav tabule
/// (<c>TSTA</c>) a stav kazdej fronty (<c>RSTA</c>). Hlasenia sa "prehraju" hned - prvu odpoved hlasi ako prijate,
/// dalsiu ako dohrate.
/// </summary>
internal sealed class RemoteProtocol : ILineProtocol
{
    /// <summary>Vyrobcovia tabul, ktore simulator za vzdialenym INISSom zobrazi; prvy je predvoleny.</summary>
    public static readonly string[] Manufacturers = ["ELEN", "ELENOLD", "ELEN10", "ELEN16", "ELEN16Kam", "ELEKON", "LCD1", "ERP", "LCD", "FERS", "ERS"];

    /// <summary>Stav tabule: zapis v poriadku.</summary>
    public const int TableOk = 0x00;

    /// <summary>Stav tabule: tabula neodpovedala.</summary>
    public const int TableNoAnswer = 0x0C;

    /// <summary>Stav tabule: odmietla spravu (ELEN NAK, Elektrocas chybna sprava, dlzka alebo suradnice).</summary>
    public const int TableRejected = 0x10;

    /// <summary>Stav tabule: odpovedala nezmyslom.</summary>
    public const int TableBadAnswer = 0x11;

    /// <summary>Stav tabule: vzdialeny INISS nema linku, na ktoru by tabulu poslal (vyrobca, ktoreho simulator nezobrazi).</summary>
    public const int TableNoLine = 0x19;

    private const string QueueName = "QNAM";
    private const string QueueCommand = "RCMD";
    private const string Recordings = "RWAV";
    private const string Title = "RTIT";
    private const string MixerLine = "LINE";
    private const string TableManufacturer = "TCLS";
    private const string TableAddress = "TADR";
    private const string TableCommand = "TCMD";
    private const string QueueStatus = "RSTA";
    private const string TableStatus = "TSTA";

    // prikaz fronty: pokracovat v hlaseni, ziadost o zapnutie/vypnutie a vypnuta fronta
    private const int KeepPlaying = 0x1;
    private const int SwitchRequest = 0x10000000;
    private const int SwitchOff = 0x20000000;
    private const int SwitchBits = SwitchRequest | SwitchOff;

    // stav fronty: hlasenie prijate (zoznam na prehratie nie je prazdny), ozvena KeepPlaying, nizsi bajt
    private const int Playing = 0x100;
    private const int KeepPlayingEcho = 0x400;
    private const int QueueIdle = 1;
    private const int QueueOff = 2;
    private const int QueueWaiting = 3;

    /// <summary>Adresa "bez prikazu pre tabulu" (hromadny prikaz ma -1).</summary>
    private const int NoTable = -2;

    private static readonly TimeSpan ReceiveGap = TimeSpan.FromSeconds(1);

    private readonly RemoteFramer _framer = new();
    private readonly ElenProtocol _elen = new();
    private readonly ElektrocasProtocol _elektrocas = new();
    private readonly ErsProtocol _ers = new();

    // fronty, ktore riadiaci INISS vypol (podla nazvu, "" = jedina fronta bez QNAM); prezije nove spojenie
    private readonly HashSet<string> _switchedOff = new(StringComparer.Ordinal);
    private DateTimeOffset _lastData;

    public bool MirrorsInnerFrames => true;

    public void Reset()
    {
        _framer.Reset();
        _elen.Reset();
        _elektrocas.Reset();
        _ers.Reset();
    }

    public BoardDisplay CreateDisplay(string manufacturer) => Inner(manufacturer).CreateDisplay(manufacturer);

    /// <summary>Poruchu tabule uplatnuje vnutorny protokol jej rodiny - tu len pre uplnost rozhrania.</summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal) => _elen.FaultReply(fault, normal);

    public void Receive(ReadOnlySpan<byte> data, ILineContext context)
    {
        var discarded = _framer.DiscardedBytes;
        if (_framer.Pending > 0 && context.Now - _lastData > ReceiveGap)
            _framer.DropPending();
        _lastData = context.Now;

        foreach (var message in _framer.Push(data))
            Handle(message, context);

        if (_framer.DiscardedBytes != discarded)
        {
            context.Line.Stats.Discarded += _framer.DiscardedBytes - discarded;
            context.LogReceived([], null, FrameStatus.Noise, new FrameInfo("noise", Args(("count", Num(_framer.DiscardedBytes - discarded)))));
        }
    }

    /// <summary>Vyrobca patri do rodiny Elektrocas (aj listove FERS).</summary>
    private static bool IsElektrocas(string? manufacturer) => manufacturer is "LCD" or "LCD1" or "ERP" or "FERS";

    private ILineProtocol Inner(string? manufacturer) => manufacturer == "ERS" ? _ers : IsElektrocas(manufacturer) ? _elektrocas : _elen;

    private void Handle(RemoteMessage message, ILineContext context)
    {
        var seq = Num(message.Sequence);
        if (!message.CrcOk)
        {
            // vzdialeny INISS spravu so zlym suctom zahodi a neodpovie
            context.Line.Stats.Frames++;
            context.Line.Stats.BadFrames++;
            context.LogReceived(message.Raw, null, FrameStatus.BadChecksum, new FrameInfo("remote.bad", Args(("seq", seq))));
            return;
        }

        var reply = new List<RemoteChunk>();
        var logged = false;
        foreach (var queue in Queues(message))
        {
            if (queue.Name is { } name)
                reply.Add(new RemoteChunk(QueueName, RemoteManufacturers.Bytes(name)));
            reply.Add(RemoteChunk.FromInt32(QueueStatus, QueueState(queue)));
            if (queue.Recordings is not null)
            {
                logged = true;
                context.LogReceived(message.Raw, null, FrameStatus.Ok, new FrameInfo("remote.report",
                    Args(("seq", seq), ("queue", queue.Name ?? ""), ("title", queue.Title ?? ""), ("recordings", queue.Recordings),
                        ("mixer", queue.MixerLine ?? ""))));
            }

            if ((queue.Command & SwitchRequest) != 0)
            {
                logged = true;
                context.LogReceived(message.Raw, null, FrameStatus.Ok,
                    new FrameInfo("remote.queue", Args(("seq", seq), ("queue", queue.Name ?? ""), ("off", (queue.Command & SwitchOff) != 0 ? "1" : "0"))));
            }
        }

        var table = Table(message, context, seq);
        if (table is { } t)
            reply.Add(RemoteChunk.FromInt32(TableStatus, t.Code | t.Length << 16));
        else
            context.Line.Stats.Frames++;

        var info = table is { } r
            ? new FrameInfo("remote.reply", Args(("seq", seq), ("code", r.Code.ToString("X2", CultureInfo.InvariantCulture)), ("result", Result(r.Code))))
            : new FrameInfo("remote.reply", Args(("seq", seq)));
        // pravidelny dotaz bez hlasenia a tabule do zaznamu nejde (pri frontach na linke chodi kazdu sekundu)
        context.ReplyRaw(table?.Address, RemoteMessage.Build(RemoteMessage.Reply, message.Sequence, reply), table?.Delay ?? TimeSpan.Zero,
            table?.Status ?? FrameStatus.Ok, info, logged || table is not null);
    }

    /// <summary>Stav fronty pre <c>RSTA</c>; zapamata si zapnutie alebo vypnutie fronty.</summary>
    private int QueueState(RemoteQueue queue)
    {
        var key = queue.Name ?? "";
        var command = queue.Command;
        if ((command & SwitchRequest) != 0 && (command & SwitchOff) != 0)
            _switchedOff.Add(key);
        else if ((command & SwitchRequest) != 0)
            _switchedOff.Remove(key);

        int state;
        if (_switchedOff.Contains(key))
            state = QueueOff;
        else if (queue.Recordings is not null)
            state = Playing | QueueWaiting;
        else
            state = QueueIdle;

        // poziadavku o zapnutie/vypnutie potvrdi ozvenou bitov, riadiaci INISS ju potom prestane posielat
        state |= command & SwitchBits;
        if ((command & KeepPlaying) != 0)
            state |= KeepPlayingEcho;
        return state;
    }

    /// <summary>Fronty v poradi spravy: <c>[QNAM] RCMD [RWAV [RTIT] [LINE]]</c>.</summary>
    private static List<RemoteQueue> Queues(RemoteMessage message)
    {
        var queues = new List<RemoteQueue>();
        RemoteQueue? current = null;
        foreach (var chunk in message.Chunks)
        {
            switch (chunk.Tag)
            {
                case QueueName:
                    current = new RemoteQueue { Name = RemoteManufacturers.Text(chunk.Data) };
                    queues.Add(current);
                    break;
                case QueueCommand:
                    if (current is null || current.HasCommand)
                    {
                        current = new RemoteQueue();
                        queues.Add(current);
                    }

                    current.Command = chunk.Int32 ?? 0;
                    current.HasCommand = true;
                    break;
                case Recordings when current is not null:
                    current.Recordings = RemoteManufacturers.Text(chunk.Data);
                    break;
                case Title when current is not null:
                    current.Title = RemoteManufacturers.Text(chunk.Data);
                    break;
                case MixerLine when current is not null:
                    current.MixerLine = RemoteManufacturers.Text(chunk.Data);
                    break;
            }
        }

        return queues;
    }

    /// <summary>Prikaz pre tabulu: dokonci spravu, spracuje ju a zisti stav pre <c>TSTA</c>; null = sprava ho nema.</summary>
    private TableResult? Table(RemoteMessage message, ILineContext context, string seq)
    {
        var code = message.Find(TableManufacturer)?.Int32;
        var address = message.Find(TableAddress)?.Int32;
        var command = message.Find(TableCommand)?.Data;
        if (code is null or -1 || address is null or NoTable || command is not { Length: > 0 })
            return null;

        int? boardAddress = address >= 0 ? address : null;
        var manufacturer = RemoteManufacturers.Name(code.Value);
        if (manufacturer is null || !Manufacturers.Contains(manufacturer, StringComparer.Ordinal))
        {
            context.Line.Stats.Frames++;
            context.LogReceived(message.Raw, boardAddress, FrameStatus.NoBoard,
                new FrameInfo("remote.unsupported", Args(("seq", seq), ("manufacturer", manufacturer ?? Num(code.Value)))));
            return new TableResult(boardAddress, TableNoLine, command.Length, TimeSpan.Zero, FrameStatus.NoBoard);
        }

        var inner = Inner(manufacturer);
        var frame = inner == _ers ? command : Finish(inner == _elektrocas, command);
        context.Forward(frame);
        var innerContext = new InnerContext(context, inner, message.Raw, seq, manufacturer);
        inner.Reset();
        inner.Receive(frame, innerContext);
        if (!innerContext.Logged)
        {
            context.Line.Stats.Frames++;
            context.LogReceived(message.Raw, boardAddress, FrameStatus.Noise, new FrameInfo("remote.empty", Args(("seq", seq), ("manufacturer", manufacturer))));
        }

        if (innerContext.Answer is { } answer)
            return new TableResult(boardAddress, inner == _ers ? ErsAnswerCode(answer.Bytes) : AnswerCode(inner == _elektrocas, answer.Bytes), frame.Length,
                answer.Delay, answer.Status);
        // hromadny prikaz a cas su bez odpovede; tabula, ktora neexistuje alebo mlci, neodpovie
        return innerContext.Faulted || innerContext.NoBoard
            ? new TableResult(boardAddress, TableNoAnswer, frame.Length, TimeSpan.Zero, innerContext.Faulted ? FrameStatus.Fault : FrameStatus.NoBoard)
            : new TableResult(boardAddress, TableOk, frame.Length, TimeSpan.Zero, FrameStatus.Ok);
    }

    /// <summary>
    /// Doplni kontrolny sucet, ktory riadiaci INISS nevyplni (sprava este nepresla jeho linkou): ELEN dva bajty za
    /// <c>04</c>, Elektrocas posledny bajt.
    /// </summary>
    private static byte[] Finish(bool elektrocas, byte[] command)
    {
        var frame = (byte[])command.Clone();
        if (elektrocas && frame.Length >= ElektrocasFrame.MinLength)
            frame[^1] = ElektrocasFrame.Checksum(frame.AsSpan(0, frame.Length - 1));
        else if (!elektrocas && frame.Length >= 5 && frame[0] == ElenFrame.Soh)
            (frame[^2], frame[^1]) = ElenChecksum.Encode(ElenChecksum.Compute(frame.AsSpan(0, frame.Length - 2)));
        return frame;
    }

    /// <summary>Stav zapisu podla odpovede tabule (ako ho zisti ovladac vzdialeneho INISSu).</summary>
    private static int AnswerCode(bool elektrocas, byte[] reply)
    {
        if (!elektrocas)
        {
            return reply switch
            {
                [ElenFrame.Nak] => TableRejected,
                [ElenFrame.Ack] or [0x7F] or [0x02, ..] => TableOk,
                _ => TableBadAnswer
            };
        }

        if (reply.Length < ElektrocasFrame.MinLength)
            return TableBadAnswer;
        if (reply[3] != ElektrocasProtocol.ReplyStatus || reply.Length < ElektrocasFrame.MinLength + 1)
            return TableOk;

        // stavove bity tabule - plati posledny najdeny v tomto poradi
        var status = reply[4];
        var code = TableOk;
        foreach (var (bit, value) in ElektrocasStatusCodes)
        {
            if ((status & bit) != 0)
                code = value;
        }

        return code;
    }

    /// <summary>
    /// Stav zapisu ERS: ACK a CAN (listy sa este tocia - vzdialeny INISS by cakal) su v poriadku, NAK a chybovy stav
    /// tabula zapis odmietla (presne kody vzdialeneho INISSu pre ERS nie su zistene).
    /// </summary>
    private static int ErsAnswerCode(byte[] reply) => reply switch
    {
        [ErsFrame.Syn, ErsFrame.Syn, _, _, ErsFrame.Ack or ErsFrame.Can] => TableOk,
        [ErsFrame.Syn, ErsFrame.Syn, _, _, ErsFrame.Nak] => TableRejected,
        [ErsFrame.Syn, ErsFrame.Syn, _, _, ErsFrame.Stx, _, _, _, (byte)'0', (byte)'1', ..] => TableOk,
        [ErsFrame.Syn, ErsFrame.Syn, _, _, ErsFrame.Stx, ..] => TableRejected,
        _ => TableBadAnswer
    };

    private static readonly (int Bit, int Code)[] ElektrocasStatusCodes =
    [
        (0x08, 0x17), // porucha izolacie 110 V
        (0x01, TableRejected), // chybna sprava
        (0x02, TableRejected), // chybna dlzka
        (0x10, TableRejected), // chybne suradnice
        (0x20, 0x16), // chyba otacania
        (0x40, 0x15), // chyba napajania 48 V / 110 V
        (0x80, 0x14) // chyba hardveru
    ];

    private static string Result(int code) => code switch
    {
        TableOk or 0x17 => "ok",
        TableNoAnswer => "noAnswer",
        TableRejected => "rejected",
        TableBadAnswer => "badAnswer",
        TableNoLine => "noLine",
        _ => "error"
    };

    private static string Num(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);

    /// <summary>Fronta hlaseni v sprave riadiaceho INISSu.</summary>
    private sealed class RemoteQueue
    {
        public string? Name { get; init; }
        public int Command { get; set; }
        public bool HasCommand { get; set; }
        public string? Recordings { get; set; }
        public string? Title { get; set; }
        public string? MixerLine { get; set; }
    }

    /// <summary>Vysledok prikazu pre tabulu.</summary>
    private readonly record struct TableResult(int? Address, int Code, int Length, TimeSpan Delay, FrameStatus Status);

    /// <summary>
    /// Kontext vnutornej spravy tabule: zaznam ide s celou spravou vzdialeneho INISSu, odpoved tabule sa zachyti (ide
    /// do <c>TSTA</c>), samoucenie zaklada tabule vyrobcu z <c>TCLS</c> a tabula inej rodiny sa ako ciel neberie.
    /// </summary>
    private sealed class InnerContext(ILineContext outer, ILineProtocol inner, byte[] raw, string seq, string tableManufacturer) : ILineContext
    {
        /// <summary>Doklad o inom vyrobcovi tabule (texty na webe <c>mismatch.remote.manufacturer</c>).</summary>
        private const string Mismatch = "remote.manufacturer";

        private readonly Type _family = inner.CreateDisplay(tableManufacturer).GetType();

        public (byte[] Bytes, TimeSpan Delay, FrameStatus Status)? Answer { get; private set; }

        /// <summary>Tabula neodpovedala pre predstieranu poruchu.</summary>
        public bool Faulted { get; private set; }

        /// <summary>Na adrese nie je tabula tejto rodiny.</summary>
        public bool NoBoard { get; private set; }

        /// <summary>Sprava tabule sa zapisala do zaznamu.</summary>
        public bool Logged { get; private set; }

        public SimLine Line => outer.Line;

        public DateTimeOffset Now => outer.Now;

        public IReadOnlyList<SimBoard> Boards => outer.Boards.Where(b => b.Display.GetType() == _family).ToList();

        public SimBoard? FindBoard(int address, string? manufacturer = null)
        {
            var board = outer.FindBoard(address, manufacturer ?? tableManufacturer);
            if (board is null)
                return null;
            // riadiaci INISS vie presneho vyrobcu - tabula s inym ho dostane ako navrh na prepnutie
            var known = board.Mismatch is { Evidence: Mismatch } m && m.Suggested.SequenceEqual([tableManufacturer]);
            if (!string.Equals(board.ManufacturerKey, tableManufacturer, StringComparison.OrdinalIgnoreCase) && !known)
            {
                board.Mismatch = new ManufacturerMismatch(Mismatch, [tableManufacturer]);
                outer.Changed(board);
            }

            return board.Display.GetType() == _family ? board : null;
        }

        public void Changed(SimBoard board) => outer.Changed(board);

        public void LogReceived(byte[] bytes, int? address, FrameStatus status, FrameInfo info)
        {
            Logged = true;
            if (status == FrameStatus.NoBoard)
                NoBoard = true;
            var args = new Dictionary<string, string>(info.Args ?? new Dictionary<string, string>()) { ["seq"] = seq, ["manufacturer"] = tableManufacturer };
            outer.LogReceived(bytes.Length > 0 ? raw : bytes, address, status, info with { Args = args });
        }

        public void Reply(SimBoard board, byte[] bytes, FrameInfo info, bool positive = true)
        {
            if (outer.Line.Mode == LineMode.Monitor)
                return;
            var reply = BoardFaults.Apply(board, inner, bytes, info, positive);
            if (reply.Bytes == null)
                Faulted = true;
            else
                Answer = (reply.Bytes, reply.Delay, reply.Status);
        }

        public void ReplyRaw(int? address, byte[] bytes, TimeSpan delay, FrameStatus status, FrameInfo info, bool log = true) =>
            Answer = (bytes, delay, status);

        public void Forward(byte[] frame) => outer.Forward(frame);
    }
}
