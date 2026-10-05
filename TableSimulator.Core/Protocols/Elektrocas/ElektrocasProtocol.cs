using System.Globalization;
using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols.Elektrocas;

/// <summary>
/// Protokol tabul Elektrocas z pohladu tabule: LCD1 a slepecke hlasice ERP (TableClass 5), starsie LCD (TableClass 1).
/// Kazdu spravu tabula potvrdi stavovou spravou <c>70</c>; znaky su v kodovani Windows-1250, kazdy s vlastnym pismom.
/// Viz docs/iniss/protokoly/lcd1.mdx.
/// </summary>
internal sealed class ElektrocasProtocol : ILineProtocol
{
    private const int CmdCheck = 0x01;
    private const int CmdType = 0x08;
    private const int CmdRowText = 0x10;
    private const int CmdRowSegments = 0x15;
    private const int CmdRowFonts = 0x16;
    private const int CmdInsertRow = 0x30;
    private const int CmdDeleteRow = 0x31;
    private const int CmdClear = 0x40;
    private const int CmdClear2 = 0x41;
    private const int CmdLight = 0x50;
    private const int CmdClock = 0x80;
    private const int CmdTimeQuery = 0xA1;
    private const int CmdTime = 0xA8;

    private const int ReplyStatus = 0x70;
    private const int ReplyType = 0x78;

    /// <summary>Stav: chybna sprava (zly sucet).</summary>
    public const int StatusBadMessage = 0x01;

    /// <summary>Stav: chybna dlzka spravy.</summary>
    public const int StatusBadLength = 0x02;

    /// <summary>Stav: chybne suradnice (riadok alebo stlpec).</summary>
    public const int StatusBadCoordinates = 0x10;

    /// <summary>Kam tabula s hodinami posiela cas (predvolene <c>RTCBroadcastAddr</c> INISSu).</summary>
    private const int RtcBroadcastAddress = 0xFA;

    /// <summary>Predvolene pismo znakov bez pisma (LCD, prikaz 10).</summary>
    private const byte DefaultFont = 0x10;

    /// <summary>Typ zariadenia v odpovedi na prikaz 08 - typ 9 smie zapis 15 pri kazdom firmveri.</summary>
    private const byte DeviceType = 9;

    /// <summary>
    /// Medzera v prijme, po ktorej tabula zahodi rozpracovanu spravu - hranicu spravy urcuje len dlzka, takze sum pred
    /// spravou by inak posunul vsetky dalsie. INISS po kazdej sprave caka na odpoved, spravy teda idu s odstupom.
    /// </summary>
    internal static readonly TimeSpan ReceiveGap = TimeSpan.FromMilliseconds(200);

    private readonly ElektrocasFramer _framer = new();
    private DateTimeOffset _lastData;

    public void Reset() => _framer.Reset();

    public BoardDisplay CreateDisplay() => new CharDisplay();

    /// <summary>Pri poruche tabula hlasi chybnu spravu (NAK), zvolene stavove bity alebo odpovie nezmyslom.</summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal)
    {
        if (fault.Kind == FaultKind.Garbage || normal.Length < ElektrocasFrame.MinLength)
            return ([0x00], new FrameInfo("garbage"));

        var status = fault.Kind == FaultKind.Status ? fault.Status & 0xFF : StatusBadMessage;
        var reply = ElektrocasFrame.Build(normal[0], normal[2], ReplyStatus, [(byte)status]);
        return (reply, StatusInfo(status));
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

    private static void Handle(ElektrocasFrame frame, ILineContext context)
    {
        context.Line.Stats.Frames++;
        if (!frame.ChecksumOk)
            context.Line.Stats.BadFrames++;

        // cas ide na adresu RTCBroadcastAddr - nastavi hodiny vsetkych tabul na linke
        if (frame.Command == CmdTime)
        {
            var clock = ParseTime(frame.Parameters);
            if (frame.ChecksumOk && clock != null)
            {
                foreach (var board in context.Boards.Where(b => b.Display is CharDisplay))
                {
                    board.Display.ClockValue = clock;
                    board.Display.ClockSetAt = context.Now;
                    context.Changed(board);
                }
            }

            context.LogReceived(frame.Raw, frame.Address, frame.ChecksumOk ? FrameStatus.Ok : FrameStatus.BadChecksum,
                new FrameInfo("lcd.time", Args(("time", clock?.ToString("yyyy-MM-dd HH:mm:ss.ff", CultureInfo.InvariantCulture) ?? ""))));
            return;
        }

        if (frame.Address == ElektrocasFrame.Broadcast)
        {
            if (frame.ChecksumOk)
            {
                foreach (var board in context.Boards.Where(b => b.Display is CharDisplay))
                {
                    Apply(board, frame, out _);
                    context.Changed(board);
                }
            }

            var info = Describe(frame, context.Line.Manufacturer);
            context.LogReceived(frame.Raw, null, frame.ChecksumOk ? FrameStatus.Ok : FrameStatus.BadChecksum, info with
            {
                Args = new Dictionary<string, string>(info.Args ?? new Dictionary<string, string>()) { ["broadcast"] = "1" }
            });
            return;
        }

        var target = context.FindBoard(frame.Address);
        if (target == null)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.NoBoard, Describe(frame, context.Line.Manufacturer));
            return;
        }

        target.Stats.Frames++;
        target.Stats.LastFrameAt = context.Now;

        if (!frame.ChecksumOk)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.BadChecksum, Describe(frame, target.ManufacturerKey));
            context.Reply(target, Status(frame, StatusBadMessage), StatusInfo(StatusBadMessage), false);
            return;
        }

        if (Mismatch(target.ManufacturerKey, frame.Command) is { } mismatch)
            target.Mismatch = mismatch;
        var status = Apply(target, frame, out var data);
        context.Changed(target);
        context.LogReceived(frame.Raw, frame.Address, FrameStatus.Ok, Describe(frame, target.ManufacturerKey));

        switch (frame.Command)
        {
            case CmdType:
                // n, typ, firmver, rezerva
                context.Reply(target, ElektrocasFrame.Build(frame.Sender, frame.Address, ReplyType, [3, DeviceType, 1, 0]),
                    new FrameInfo("lcd.typeReply", Args(("type", Num(DeviceType)))));
                break;
            case CmdTimeQuery:
                context.Reply(target, ElektrocasFrame.Build(RtcBroadcastAddress, frame.Address, CmdTime, data ?? []), new FrameInfo("lcd.timeReply"));
                break;
            default:
                context.Reply(target, Status(frame, status), StatusInfo(status), status == 0);
                break;
        }
    }

    /// <summary>Vykona prikaz na tabuli; vrati stav pre odpoved a pri dotaze na cas obsah casovej spravy.</summary>
    private static int Apply(SimBoard board, ElektrocasFrame frame, out byte[]? data)
    {
        data = null;
        var display = (CharDisplay)board.Display;
        var p = frame.Parameters;
        switch (frame.Command)
        {
            case CmdRowText when p.Length >= 2:
            {
                var text = p.AsSpan(2);
                return Write(board, display, p[0], p[1], 0, text, [DefaultFont]);
            }

            case CmdRowFonts when p.Length >= 3:
            {
                int row = p[0], column = p[1], layers = p[2];
                if (layers <= 0 || (p.Length - 3) % layers != 0)
                    return StatusBadLength;
                var block = (p.Length - 3) / layers;
                if (block < 1 || (block - 1) % 2 != 0)
                    return StatusBadLength;
                var n = (block - 1) / 2;
                for (var i = 0; i < layers; i++)
                {
                    var b = p.AsSpan(3 + i * block, block);
                    var status = Write(board, display, row, column, b[0], b.Slice(1, n), b.Slice(1 + n, n));
                    if (status != 0)
                        return status;
                }

                return 0;
            }

            case CmdRowSegments when p.Length >= 2:
            {
                int row = p[0];
                var i = 1;
                while (i + 4 <= p.Length)
                {
                    int column = p[i], layer = p[i + 1], count = p[i + 3];
                    var font = p[i + 2];
                    if (i + 4 + count > p.Length)
                        return StatusBadLength;
                    var status = Write(board, display, row, column, layer, p.AsSpan(i + 4, count), [font]);
                    if (status != 0)
                        return status;
                    i += 4 + count;
                }

                return i == p.Length ? 0 : StatusBadLength;
            }

            case CmdInsertRow when p.Length >= 1:
                display.InsertRow(p[0], board.Profile.Rows);
                return 0;

            case CmdDeleteRow when p.Length >= 1:
                display.DeleteRow(p[0]);
                return 0;

            case CmdClear or CmdClear2:
                display.Clear();
                return 0;

            case CmdLight when p.Length >= 1:
                display.Brightness = p[0] == 0 ? 0 : 100;
                return 0;

            case CmdTimeQuery:
                data = TimeBytes(display.ClockValue is { } c && display.ClockSetAt is { } at
                    ? c + (DateTimeOffset.UtcNow - at)
                    : DateTime.Now);
                return 0;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Sprava, ktoru INISS posiela len inemu vyrobcovi: zapis bez pisma <c>10</c> len tabuliam LCD, zapis s pismom
    /// <c>15</c>/<c>16</c> a dotaz na typ <c>08</c> len LCD1 a ERP.
    /// </summary>
    private static ManufacturerMismatch? Mismatch(string manufacturer, int command)
    {
        var lcd = string.Equals(manufacturer, "LCD", StringComparison.OrdinalIgnoreCase);
        return command switch
        {
            CmdRowSegments or CmdRowFonts or CmdType when lcd => new ManufacturerMismatch("lcd.lcd1Commands", ["LCD1", "ERP"]),
            CmdRowText when !lcd => new ManufacturerMismatch("lcd.lcdCommands", ["LCD"]),
            _ => null
        };
    }

    /// <summary>Zapise znaky; riadok mimo tabule je chybna suradnica (pri samouceni sa tabula zvacsi).</summary>
    private static int Write(SimBoard board, CharDisplay display, int row, int column, int layer, ReadOnlySpan<byte> codes, ReadOnlySpan<byte> fonts)
    {
        var profile = board.Profile;
        if (profile.AutoSize)
        {
            var width = Math.Max(profile.Width, (column + codes.Length) * 8);
            if (row + 1 > profile.Rows || width > profile.Width)
                board.Profile = profile with { Rows = Math.Max(profile.Rows, row + 1), Width = width };
        }
        else if (row >= profile.Rows || column + codes.Length > Math.Max(1, profile.Width / 8))
        {
            return StatusBadCoordinates;
        }

        display.Row(row).Write(layer, column, codes, fonts);
        return 0;
    }

    private static byte[] Status(ElektrocasFrame frame, int status) =>
        ElektrocasFrame.Build(frame.Sender, frame.Address, ReplyStatus, [(byte)status]);

    private static FrameInfo StatusInfo(int status) =>
        new(status == 0 ? "lcd.ok" : "lcd.status", Args(("status", status.ToString("X2", CultureInfo.InvariantCulture))));

    /// <summary>Vyznam spravy pre zaznam komunikacie.</summary>
    private static FrameInfo Describe(ElektrocasFrame frame, string manufacturer)
    {
        var p = frame.Parameters;
        switch (frame.Command)
        {
            case CmdRowText when p.Length >= 2:
                return new FrameInfo("lcd.row", Args(("row", Num(p[0])), ("col", Num(p[1])), ("layer", "0"), ("text", ElektrocasCharset.Decode(p.AsSpan(2), manufacturer))));
            case CmdRowFonts when p.Length >= 3 && p[2] > 0 && (p.Length - 3) % p[2] == 0:
            {
                var block = (p.Length - 3) / p[2];
                var n = (block - 1) / 2;
                var parts = Enumerable.Range(0, p[2]).Select(i => ElektrocasCharset.Decode(p.AsSpan(3 + i * block + 1, n), manufacturer));
                return new FrameInfo("lcd.row", Args(("row", Num(p[0])), ("col", Num(p[1])),
                    ("layer", Num(p[3])), ("text", string.Join(" | ", parts))));
            }
            case CmdRowSegments when p.Length >= 2:
                return new FrameInfo("lcd.segments", Args(("row", Num(p[0]))));
            case CmdInsertRow when p.Length >= 1:
                return new FrameInfo("lcd.insert", Args(("row", Num(p[0]))));
            case CmdDeleteRow when p.Length >= 1:
                return new FrameInfo("lcd.delete", Args(("row", Num(p[0]))));
            case CmdClear or CmdClear2:
                return new FrameInfo("lcd.clear");
            case CmdLight when p.Length >= 1:
                return new FrameInfo("lcd.light", Args(("value", Num(p[0]))));
            case CmdCheck:
                return new FrameInfo("lcd.check");
            case CmdType:
                return new FrameInfo("lcd.type");
            case CmdClock:
                return new FrameInfo("lcd.clock");
            case CmdTimeQuery:
                return new FrameInfo("lcd.timeQuery");
            default:
                return new FrameInfo("lcd.unknown", Args(("cmd", frame.Command.ToString("X2", CultureInfo.InvariantCulture))));
        }
    }

    /// <summary>Cas zo spravy A8: <c>st ss mm hh DD tyzden|MM RR priznaky</c> v BCD; null pri zlom tvare.</summary>
    internal static DateTime? ParseTime(byte[] p)
    {
        if (p.Length < 7)
            return null;
        int Bcd(byte b) => (b >> 4) * 10 + (b & 0x0F);
        try
        {
            return new DateTime(2000 + Bcd(p[6]), Bcd((byte)(p[5] & 0x1F)), Bcd(p[4]), Bcd(p[3]), Bcd(p[2]), Bcd(p[1]), Bcd(p[0]) * 10,
                DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Obsah casovej spravy A8 (hodiny na linke): BCD, den v tyzdni 1 pondelok az 7 nedela, priznaky 50 (51 v lete).</summary>
    internal static byte[] TimeBytes(DateTime time)
    {
        static byte Bcd(int v) => (byte)((v / 10 << 4) | (v % 10));
        var dayOfWeek = time.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)time.DayOfWeek;
        return
        [
            Bcd(time.Millisecond / 10), Bcd(time.Second), Bcd(time.Minute), Bcd(time.Hour), Bcd(time.Day),
            (byte)((dayOfWeek << 5) | Bcd(time.Month)), Bcd(time.Year % 100), (byte)(TimeZoneInfo.Local.IsDaylightSavingTime(time) ? 0x51 : 0x50)
        ];
    }

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);
}
