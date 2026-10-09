using System.Globalization;
using System.Text;
using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols.Ers;

/// <summary>
/// Sprava ERS od INISSu: zapis riadku <c>16 16 01 adresa 02 'D' 'L' nnn text 03 BCC</c> alebo dotaz
/// <c>16 16 01 adresa 05</c>.
/// </summary>
/// <param name="Address">adresa tabule</param>
/// <param name="Command">prikaz (<c>DL</c>); null pri dotaze</param>
/// <param name="Data">znaky za prikazom (cislo riadku a kody listov); prazdne pri dotaze</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">BCC sedi (dotaz nema)</param>
public sealed record ErsFrame(int Address, string? Command, byte[] Data, byte[] Raw, bool ChecksumOk)
{
    public const byte Syn = 0x16;
    public const byte Soh = 0x01;
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;
    public const byte Enq = 0x05;
    public const byte Ack = 0x06;
    public const byte Nak = 0x15;
    public const byte Can = 0x18;

    /// <summary>Prikaz zapisu riadku.</summary>
    public const string WriteRow = "DL";

    /// <summary>Dotaz, ci tabula dokoncila.</summary>
    public bool IsQuery => Command is null;

    /// <summary>Kontrolny sucet: sucet bajtov od STX po ETX vratane, dolnych 7 bitov.</summary>
    public static byte Bcc(ReadOnlySpan<byte> fromStxToEtx)
    {
        var sum = 0;
        foreach (var b in fromStxToEtx)
            sum += b;
        return (byte)(sum & 0x7F);
    }

    /// <summary>Zapis riadku (na testy a nastroje).</summary>
    public static byte[] Build(int address, int row, string codes)
    {
        var body = Encoding.ASCII.GetBytes("DL" + row.ToString("000", CultureInfo.InvariantCulture) + codes);
        byte[] raw = [Syn, Syn, Soh, (byte)address, Stx, .. body, Etx, 0];
        raw[^1] = Bcc(raw.AsSpan(4, raw.Length - 5));
        return raw;
    }

    /// <summary>Dotaz (na testy a nastroje).</summary>
    public static byte[] Query(int address) => [Syn, Syn, Soh, (byte)address, Enq];

    /// <summary>Kratka odpoved tabule: <c>16 16 01 adresa kod</c> (ACK, NAK, CAN).</summary>
    public static byte[] Short(int address, byte code) => [Syn, Syn, Soh, (byte)address, code];

    /// <summary>Stavova sprava: <c>16 16 01 adresa 02 d0…d7 03 BCC</c>, kod stavu v d3 a d4 (dve cislice ASCII).</summary>
    public static byte[] Status(int address, int code)
    {
        var digits = Encoding.ASCII.GetBytes("000" + Math.Clamp(code, 0, 99).ToString("00", CultureInfo.InvariantCulture) + "000");
        byte[] raw = [Syn, Syn, Soh, (byte)address, Stx, .. digits, Etx, 0];
        raw[^1] = Bcc(raw.AsSpan(4, raw.Length - 5));
        return raw;
    }
}

/// <summary>
/// Sklada spravy ERS z prudu bajtov: zaciatok <c>16 16 01</c>, za adresou bud <c>05</c> (dotaz), alebo text od
/// <c>02</c> po <c>03</c> a BCC. Bajty mimo sprav zahodi.
/// </summary>
public sealed class ErsFramer
{
    private const int MaxFrameLength = 512;

    private readonly List<byte> _buffer = [];

    /// <summary>Pocet bajtov mimo sprav (sum na linke), ktore sa zahodili.</summary>
    public long DiscardedBytes { get; private set; }

    /// <summary>Pocet bajtov rozpracovanej spravy.</summary>
    public int Pending => _buffer.Count;

    /// <summary>Zahodi rozpracovanu spravu.</summary>
    public void Reset() => _buffer.Clear();

    /// <summary>Zahodi rozpracovanu spravu ako sum (po medzere v prijme).</summary>
    public void DropPending()
    {
        DiscardedBytes += _buffer.Count;
        _buffer.Clear();
    }

    /// <summary>Prida prijate bajty a vrati spravy, ktore sa nimi dokoncili (aj so zlym suctom).</summary>
    public IReadOnlyList<ErsFrame> Push(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            _buffer.Add(b);

        List<ErsFrame>? frames = null;
        while (_buffer.Count > 0)
        {
            // zaciatok 16 16 01 - kym sa nezhoduje, prvy bajt je sum
            var prefix = Math.Min(3, _buffer.Count);
            if (!_buffer.Take(prefix).SequenceEqual(new byte[] { ErsFrame.Syn, ErsFrame.Syn, ErsFrame.Soh }.Take(prefix)))
            {
                Discard();
                continue;
            }

            if (_buffer.Count < 5)
                break;
            if (_buffer[4] == ErsFrame.Enq)
            {
                var raw = Take(5);
                (frames ??= []).Add(new ErsFrame(raw[3], null, [], raw, true));
                continue;
            }

            if (_buffer[4] != ErsFrame.Stx)
            {
                Discard();
                continue;
            }

            var etx = _buffer.IndexOf(ErsFrame.Etx, 5);
            if (etx < 0)
            {
                if (_buffer.Count > MaxFrameLength)
                    Discard();
                else
                    break;
                continue;
            }

            if (_buffer.Count < etx + 2)
                break;
            var frame = Take(etx + 2);
            var body = frame.AsSpan(5, etx - 5);
            var ok = ErsFrame.Bcc(frame.AsSpan(4, etx - 3)) == (frame[^1] & 0x7F);
            var command = body.Length >= 2 ? Encoding.ASCII.GetString(body[..2]) : "";
            (frames ??= []).Add(new ErsFrame(frame[3], command, body.Length >= 2 ? body[2..].ToArray() : [], frame, ok));
        }

        return frames ?? (IReadOnlyList<ErsFrame>)[];
    }

    private void Discard()
    {
        _buffer.RemoveAt(0);
        DiscardedBytes++;
    }

    private byte[] Take(int count)
    {
        var raw = _buffer.GetRange(0, count).ToArray();
        _buffer.RemoveRange(0, count);
        return raw;
    }
}

/// <summary>
/// Listove tabule ERS (TableClass 2) z pohladu tabule: zapis riadku potvrdi ACK a listy otaca; na dotaz kym sa
/// otacaju odpovie CAN, potom stavovou spravou (01 = hotovo, 02 chybne cislo riadku, 03 chybna pozicia, 04 neznamy
/// prikaz). Zly sucet odmietne NAK. Viz docs/iniss/protokoly/ers-fers.mdx.
/// </summary>
internal sealed class ErsProtocol : ILineProtocol
{
    /// <summary>Stav: chybne cislo riadku.</summary>
    public const int StatusBadRow = 2;

    /// <summary>Stav: chybna pozicia.</summary>
    public const int StatusBadPosition = 3;

    /// <summary>Stav: neznamy prikaz.</summary>
    public const int StatusUnknownCommand = 4;

    /// <summary>Stav: casovy limit (predstierana porucha bez zvoleneho kodu).</summary>
    public const int StatusTimeout = 7;

    private static readonly TimeSpan ReceiveGap = TimeSpan.FromMilliseconds(500);

    private readonly ErsFramer _framer = new();
    private DateTimeOffset _lastData;

    public void Reset() => _framer.Reset();

    public BoardDisplay CreateDisplay(string manufacturer) => new FlapDisplay();

    /// <summary>Pri poruche tabula zapis odmietne (NAK), odpovie nezmyslom alebo hlasi zvoleny stav (02-08).</summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal)
    {
        var address = normal.Length > 3 ? normal[3] : 0;
        return fault.Kind switch
        {
            FaultKind.Garbage => ([0x00], new FrameInfo("garbage")),
            FaultKind.Status => (ErsFrame.Status(address, fault.Status is >= 2 and <= 8 ? fault.Status : StatusTimeout),
                StatusInfo(fault.Status is >= 2 and <= 8 ? fault.Status : StatusTimeout)),
            _ => (ErsFrame.Short(address, ErsFrame.Nak), new FrameInfo("nak"))
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

    private static void Handle(ErsFrame frame, ILineContext context)
    {
        context.Line.Stats.Frames++;
        if (!frame.ChecksumOk)
            context.Line.Stats.BadFrames++;

        var target = context.FindBoard(frame.Address);
        if (target == null)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.NoBoard, Describe(frame));
            return;
        }

        target.Stats.Frames++;
        target.Stats.LastFrameAt = context.Now;
        if (target.Display is not FlapDisplay display)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.NoBoard, Describe(frame));
            return;
        }

        if (frame.IsQuery)
        {
            var rotating = display.IsRotating(context.Now);
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.Ok, Describe(frame));
            if (rotating)
                context.Reply(target, ErsFrame.Short(frame.Address, ErsFrame.Can), new FrameInfo("ers.busy"));
            else
                context.Reply(target, ErsFrame.Status(frame.Address, display.ErsStatus), StatusInfo(display.ErsStatus), display.ErsStatus == FlapDisplay.ErsDone);
            return;
        }

        if (!frame.ChecksumOk)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.BadChecksum, Describe(frame));
            context.Reply(target, ErsFrame.Short(frame.Address, ErsFrame.Nak), new FrameInfo("nak"), false);
            return;
        }

        display.ErsStatus = Apply(target, display, frame, context.Now);
        context.Changed(target);
        context.LogReceived(frame.Raw, frame.Address, FrameStatus.Ok, Describe(frame));
        context.Reply(target, ErsFrame.Short(frame.Address, ErsFrame.Ack), new FrameInfo("ack"));
    }

    /// <summary>Zapise riadok; vrati stav pre dalsi dotaz.</summary>
    private static int Apply(SimBoard board, FlapDisplay display, ErsFrame frame, DateTimeOffset now)
    {
        if (frame.Command != ErsFrame.WriteRow)
            return StatusUnknownCommand;
        if (frame.Data.Length < 3 || !int.TryParse(Encoding.ASCII.GetString(frame.Data, 0, 3), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
            number < 1)
            return StatusBadRow;

        var row = number - 1;
        var codes = Encoding.ASCII.GetString(frame.Data, 3, frame.Data.Length - 3);
        var profile = board.Profile;
        if (profile.AutoSize)
        {
            var width = Math.Max(profile.Width, codes.Length * 8);
            if (row + 1 > profile.Rows || width > profile.Width)
                board.Profile = profile with { Rows = Math.Max(profile.Rows, row + 1), Width = width };
        }
        else if (row >= profile.Rows)
        {
            return StatusBadRow;
        }
        else if (codes.Length > Math.Max(1, profile.Width / 8))
        {
            return StatusBadPosition;
        }

        if (display.Write(row, 0, codes))
            display.RotatingUntil = now.AddMilliseconds(board.Profile.RotationMs);
        return FlapDisplay.ErsDone;
    }

    private static FrameInfo Describe(ErsFrame frame)
    {
        if (frame.IsQuery)
            return new FrameInfo("ers.query");
        if (frame.Command == ErsFrame.WriteRow && frame.Data.Length >= 3)
        {
            return new FrameInfo("ers.row",
                Args(("row", Encoding.ASCII.GetString(frame.Data, 0, 3).TrimStart('0')), ("codes", Encoding.ASCII.GetString(frame.Data, 3, frame.Data.Length - 3))));
        }

        return new FrameInfo("ers.unknown", Args(("cmd", frame.Command ?? "")));
    }

    private static FrameInfo StatusInfo(int status) =>
        new(status == FlapDisplay.ErsDone ? "ers.done" : "ers.status", Args(("status", status.ToString("00", CultureInfo.InvariantCulture))));

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);
}
