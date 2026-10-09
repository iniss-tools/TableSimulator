using System.Globalization;
using TableSimulator.Core.Boards;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Protocols.Apel;

/// <summary>Zaznam spravy APEL: prikaz (pri zapise textu cislo zasobnika alebo riadku) a jeho data.</summary>
/// <param name="Command">prikaz</param>
/// <param name="Data">data</param>
public sealed record ApelRecord(int Command, byte[] Data);

/// <summary>
/// Sprava APEL od INISSu: <c>10 02 adresa [prikaz dlzka data…]… sucet 10 03</c> - bajt <c>10</c> vnutri spravy je
/// zdvojeny, sucet je XOR vsetkych bajtov od prvého prikazu po koniec dat (bez adresy). Jedna sprava moze niest viac
/// zaznamov (zmazanie = prazdny text do viacerych zasobnikov).
/// </summary>
/// <param name="Address">adresa tabule; <c>FF</c> = vsetky</param>
/// <param name="Records">zaznamy</param>
/// <param name="Raw">cela sprava, ako prisla (so zdvojenymi bajtmi)</param>
/// <param name="ChecksumOk">sucet sedi a zaznamy presne vyplnia spravu</param>
public sealed record ApelFrame(int Address, IReadOnlyList<ApelRecord> Records, byte[] Raw, bool ChecksumOk)
{
    public const byte Dle = 0x10;
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;

    /// <summary>Adresa vsetkych tabul.</summary>
    public const int Broadcast = 0xFF;

    /// <summary>Predvolena adresa pocitaca (<c>ComputerAddr</c> linky triedy 10) - tabula ju vracia v odpovedi.</summary>
    public const int DefaultComputerAddress = 0x80;

    /// <summary>Stavy v odpovedi tabule.</summary>
    public const byte StateOk = 0x06;

    public const byte StateBusy = 0x07;
    public const byte StateLostConfig = 0x08;
    public const byte StateTransmissionError = 0x15;

    /// <summary>Obsah spravy bez ramca: adresa, zaznamy a sucet (na zabalenie).</summary>
    public static byte[] Payload(int address, IEnumerable<ApelRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var body = new List<byte>();
        foreach (var record in records)
        {
            body.Add((byte)record.Command);
            body.Add((byte)record.Data.Length);
            body.AddRange(record.Data);
        }

        return [(byte)address, .. body, Xor(body.ToArray())];
    }

    /// <summary>Cela sprava (na testy a nastroje).</summary>
    public static byte[] Build(int address, params ApelRecord[] records) => Wrap(Payload(address, records));

    /// <summary>Odpoved tabule: <c>10 02 ComputerAddr adresa stav sucet 10 03</c>, sucet = XOR adresy a stavu.</summary>
    public static byte[] Reply(int computerAddress, int address, byte state) =>
        Wrap([(byte)computerAddress, (byte)address, state, (byte)(address ^ state)]);

    /// <summary>Zabali obsah do <c>10 02 … 10 03</c> a zdvoji bajty <c>10</c>.</summary>
    public static byte[] Wrap(ReadOnlySpan<byte> payload)
    {
        var raw = new List<byte>(payload.Length + 6) { Dle, Stx };
        foreach (var b in payload)
        {
            raw.Add(b);
            if (b == Dle)
                raw.Add(Dle);
        }

        raw.Add(Dle);
        raw.Add(Etx);
        return raw.ToArray();
    }

    /// <summary>XOR bajtov.</summary>
    public static byte Xor(ReadOnlySpan<byte> bytes)
    {
        byte sum = 0;
        foreach (var b in bytes)
            sum ^= b;
        return sum;
    }

    /// <summary>Rozlozi obsah spravy (bez ramca a zdvojeni): adresa, zaznamy a sucet.</summary>
    public static ApelFrame Parse(ReadOnlySpan<byte> payload, byte[] raw)
    {
        if (payload.Length < 2)
            return new ApelFrame(payload.Length > 0 ? payload[0] : 0, [], raw, false);
        var body = payload[1..^1];
        var records = new List<ApelRecord>();
        var i = 0;
        var complete = true;
        while (i < body.Length)
        {
            if (i + 2 > body.Length || i + 2 + body[i + 1] > body.Length)
            {
                complete = false;
                break;
            }

            records.Add(new ApelRecord(body[i], body.Slice(i + 2, body[i + 1]).ToArray()));
            i += 2 + body[i + 1];
        }

        return new ApelFrame(payload[0], records, raw, complete && Xor(body) == payload[^1]);
    }
}

/// <summary>
/// Sklada spravy APEL z prudu bajtov: zaciatok <c>10 02</c>, koniec <c>10 03</c>, <c>10 10</c> vnutri = jeden bajt
/// <c>10</c>. Bajty mimo sprav zahodi.
/// </summary>
public sealed class ApelFramer
{
    private const int MaxFrameLength = 1024;

    private readonly List<byte> _raw = [];
    private readonly List<byte> _payload = [];
    private bool _inFrame;
    private bool _dle;

    /// <summary>Pocet bajtov mimo sprav (sum na linke), ktore sa zahodili.</summary>
    public long DiscardedBytes { get; private set; }

    /// <summary>Pocet bajtov rozpracovanej spravy.</summary>
    public int Pending => _raw.Count;

    /// <summary>Zahodi rozpracovanu spravu.</summary>
    public void Reset()
    {
        _raw.Clear();
        _payload.Clear();
        _inFrame = false;
        _dle = false;
    }

    /// <summary>Zahodi rozpracovanu spravu ako sum (po medzere v prijme).</summary>
    public void DropPending()
    {
        DiscardedBytes += _raw.Count;
        Reset();
    }

    /// <summary>Prida prijate bajty a vrati spravy, ktore sa nimi dokoncili (aj so zlym suctom).</summary>
    public IReadOnlyList<ApelFrame> Push(ReadOnlySpan<byte> data)
    {
        List<ApelFrame>? frames = null;
        foreach (var b in data)
        {
            if (!_inFrame)
            {
                // cakanie na 10 02
                if (_dle && b == ApelFrame.Stx)
                {
                    _inFrame = true;
                    _dle = false;
                    _raw.Clear();
                    _raw.AddRange([ApelFrame.Dle, ApelFrame.Stx]);
                    _payload.Clear();
                    continue;
                }

                if (_dle)
                    DiscardedBytes++;
                _dle = b == ApelFrame.Dle;
                if (!_dle)
                    DiscardedBytes++;
                continue;
            }

            _raw.Add(b);
            if (_dle)
            {
                _dle = false;
                if (b == ApelFrame.Dle)
                {
                    _payload.Add(ApelFrame.Dle);
                }
                else if (b == ApelFrame.Etx)
                {
                    (frames ??= []).Add(ApelFrame.Parse(_payload.ToArray(), _raw.ToArray()));
                    Reset();
                }
                else if (b == ApelFrame.Stx)
                {
                    // nova sprava bez konca predchadzajucej
                    DiscardedBytes += _raw.Count - 2;
                    _raw.Clear();
                    _raw.AddRange([ApelFrame.Dle, ApelFrame.Stx]);
                    _payload.Clear();
                }
                else
                {
                    _payload.Add(b);
                }

                continue;
            }

            if (b == ApelFrame.Dle)
                _dle = true;
            else
                _payload.Add(b);
            if (_raw.Count > MaxFrameLength)
                DropPending();
        }

        return frames ?? (IReadOnlyList<ApelFrame>)[];
    }
}

/// <summary>
/// Tabule APEL (TableClass 10) z pohladu tabule: jednoriadkova <c>APEL</c> ukazuje text zasobnika <c>00</c>,
/// viacriadkova <c>APEL_AN</c> riadky podla cisla - pismo a rozlozenie urcuje tabula sama, INISS posiela holy text.
/// Kazdu spravu tabula potvrdi stavom <c>06</c>; hromadne spravy (<c>FF</c>) su bez odpovede. Viz
/// docs/iniss/protokoly/apel.mdx.
/// </summary>
internal sealed class ApelProtocol : ILineProtocol
{
    private const int CmdTime = 0x81;
    private const int CmdTemperature = 0x82;
    private const int CmdLight = 0x83;
    private const int CmdCheck = 0x8A;

    /// <summary>Prikazy pod touto hodnotou su zapis textu (cislo zasobnika alebo riadku).</summary>
    private const int TextCommands = 0x80;

    private static readonly TimeSpan ReceiveGap = TimeSpan.FromMilliseconds(500);

    private readonly ApelFramer _framer = new();
    private DateTimeOffset _lastData;

    public void Reset() => _framer.Reset();

    public BoardDisplay CreateDisplay(string manufacturer) => new CharDisplay();

    /// <summary>Pri poruche tabula hlasi chybu prenosu (NAK), zvoleny stav (napr. 07 zaneprazdnena) alebo odpovie nezmyslom.</summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal)
    {
        var address = normal.Length > 3 ? normal[3] : 0;
        return fault.Kind switch
        {
            FaultKind.Garbage => ([0x00], new FrameInfo("garbage")),
            FaultKind.Status when fault.Status is > 0 and < 256 => (ApelFrame.Reply(ApelFrame.DefaultComputerAddress, address, (byte)fault.Status),
                StateInfo((byte)fault.Status)),
            _ => (ApelFrame.Reply(ApelFrame.DefaultComputerAddress, address, ApelFrame.StateTransmissionError), StateInfo(ApelFrame.StateTransmissionError))
        };
    }

    public void Receive(ReadOnlySpan<byte> data, ILineContext context)
    {
        var discarded = _framer.DiscardedBytes;
        if (_framer.Pending > 0 && context.Now - _lastData > ReceiveGap)
            _framer.DropPending();
        _lastData = context.Now;

        foreach (var frame in _framer.Push(data))
            Handle(frame, context);

        if (_framer.DiscardedBytes != discarded)
        {
            context.Line.Stats.Discarded += _framer.DiscardedBytes - discarded;
            context.LogReceived([], null, FrameStatus.Noise, new FrameInfo("noise", Args(("count", Num((int)(_framer.DiscardedBytes - discarded))))));
        }
    }

    private static void Handle(ApelFrame frame, ILineContext context)
    {
        context.Line.Stats.Frames++;
        if (!frame.ChecksumOk)
            context.Line.Stats.BadFrames++;
        var status = frame.ChecksumOk ? FrameStatus.Ok : FrameStatus.BadChecksum;

        if (frame.Address == ApelFrame.Broadcast)
        {
            if (frame.ChecksumOk)
            {
                foreach (var board in context.Boards.Where(b => b.Display is CharDisplay))
                {
                    Apply(board, frame, context.Now);
                    context.Changed(board);
                }
            }

            var info = Describe(frame);
            context.LogReceived(frame.Raw, null, status, info with
            {
                Args = new Dictionary<string, string>(info.Args ?? new Dictionary<string, string>()) { ["broadcast"] = "1" }
            });
            return;
        }

        var target = context.FindBoard(frame.Address);
        if (target == null)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.NoBoard, Describe(frame));
            return;
        }

        target.Stats.Frames++;
        target.Stats.LastFrameAt = context.Now;
        if (!frame.ChecksumOk)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.BadChecksum, Describe(frame));
            context.Reply(target, ApelFrame.Reply(ApelFrame.DefaultComputerAddress, frame.Address, ApelFrame.StateTransmissionError),
                StateInfo(ApelFrame.StateTransmissionError), false);
            return;
        }

        if (target.Display is CharDisplay)
        {
            Apply(target, frame, context.Now);
            context.Changed(target);
        }

        context.LogReceived(frame.Raw, frame.Address, FrameStatus.Ok, Describe(frame));
        context.Reply(target, ApelFrame.Reply(ApelFrame.DefaultComputerAddress, frame.Address, ApelFrame.StateOk), StateInfo(ApelFrame.StateOk));
    }

    /// <summary>Vykona zaznamy spravy na tabuli.</summary>
    private static void Apply(SimBoard board, ApelFrame frame, DateTimeOffset now)
    {
        var display = (CharDisplay)board.Display;
        var multiRow = string.Equals(board.ManufacturerKey, "APEL_AN", StringComparison.OrdinalIgnoreCase);
        foreach (var record in frame.Records)
        {
            if (record.Command < TextCommands)
            {
                // jednoriadkova tabula ukazuje zasobnik 00, ostatne zasobniky si len pamata
                if (!multiRow && record.Command != 0)
                    continue;
                WriteRow(board, display, multiRow ? record.Command : 0, record.Data);
            }
            else if (record.Command == CmdTime && ParseTime(record.Data) is { } time)
            {
                display.ClockValue = time;
                display.ClockSetAt = now;
            }
        }
    }

    /// <summary>Text celeho riadku; pri samouceni sa tabula zvacsi, aby sa zmestil.</summary>
    private static void WriteRow(SimBoard board, CharDisplay display, int row, byte[] text)
    {
        var profile = board.Profile;
        if (profile.AutoSize)
        {
            var width = Math.Max(profile.Width, text.Length * 8);
            if (row + 1 > profile.Rows || width > profile.Width)
                board.Profile = profile with { Rows = Math.Max(profile.Rows, row + 1), Width = width };
        }

        display.ClearRow(row);
        if (text.Length > 0)
            display.Row(row).Write(0, 0, text, new byte[text.Length]);
    }

    /// <summary>Cas zo spravy 81: <c>ss mm hh DD MM storocie rok</c> v BCD; null pri zlom tvare.</summary>
    internal static DateTime? ParseTime(byte[] p)
    {
        if (p.Length < 7)
            return null;
        int Bcd(byte b) => (b >> 4) * 10 + (b & 0x0F);
        try
        {
            return new DateTime(Bcd(p[5]) * 100 + Bcd(p[6]), Bcd(p[4]), Bcd(p[3]), Bcd(p[2]), Bcd(p[1]), Bcd(p[0]), DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Vyznam spravy pre zaznam komunikacie.</summary>
    private static FrameInfo Describe(ApelFrame frame)
    {
        var records = frame.Records;
        if (records.Count > 1 && records.All(r => r.Command < TextCommands && r.Data.Length == 0))
            return new FrameInfo("apel.clear", Args(("count", Num(records.Count))));
        if (records.Count > 1 && records.All(r => r.Command < TextCommands))
            return new FrameInfo("apel.texts", Args(("count", Num(records.Count)), ("text", Encodings.Win1250.GetString(records[0].Data))));
        if (records.Count == 0)
            return new FrameInfo("apel.unknown", Args(("cmd", "")));
        var record = records[0];
        return record.Command switch
        {
            < TextCommands => new FrameInfo("apel.text", Args(("row", Num(record.Command)), ("text", Encodings.Win1250.GetString(record.Data)))),
            CmdTime => new FrameInfo("apel.time",
                Args(("time", ParseTime(record.Data)?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? ""))),
            CmdLight when record.Data.Length >= 1 => new FrameInfo("apel.light", Args(("value", Num(record.Data[0])))),
            CmdCheck => new FrameInfo("apel.check"),
            CmdTemperature => new FrameInfo("apel.temperature"),
            _ => new FrameInfo("apel.unknown", Args(("cmd", record.Command.ToString("X2", CultureInfo.InvariantCulture))))
        };
    }

    private static FrameInfo StateInfo(byte state) =>
        state == ApelFrame.StateOk ? new FrameInfo("ack") : new FrameInfo("apel.state", Args(("state", state.ToString("X2", CultureInfo.InvariantCulture))));

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);
}
