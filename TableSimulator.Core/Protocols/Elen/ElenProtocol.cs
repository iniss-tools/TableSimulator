using System.Globalization;
using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols.Elen;

/// <summary>
/// Protokol ELEN (TableClass 4) z pohladu tabule: potvrdzuje spravy jednym bajtom a pamata si obsah riadkov.
/// Spravanie na strane panelu (dvojita adresa, prikazy 2, 5-9, dva jasy) podla programu ELEN Monitor.
/// </summary>
internal sealed class ElenProtocol : ILineProtocol
{
    private const int CmdTime = 0;
    private const int CmdRow = 1;
    private const int CmdReadRow = 2;
    private const int CmdMoving = 3;
    private const int CmdLight = 4;
    private const int CmdStatus = 5;
    private const int CmdReset = 7;
    private const int CmdTimeout = 8;
    private const int CmdFlash = 9;

    /// <summary>Prikazy, ktore panel len potvrdi.</summary>
    private static readonly HashSet<int> AckOnly = [6, 0x0B, 0x0D];

    /// <summary>Odpoved panelu na prikaz 7.</summary>
    private const byte ResetReply = 0x7F;

    private const byte CarriageReturn = 0x0D;

    private readonly ElenFramer _framer = new();

    public void Reset() => _framer.Reset();

    public BoardDisplay CreateDisplay(string manufacturer) => new ElenDisplay();

    /// <summary>Panel ELEN pri poruche odmietne (NAK) alebo odpovie nezmyslom; stav nehlasi - odmietne.</summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal) => fault.Kind == FaultKind.Garbage
        ? ([0x00], new FrameInfo("garbage"))
        : ([ElenFrame.Nak], new FrameInfo("nak"));

    public void Receive(ReadOnlySpan<byte> data, ILineContext context)
    {
        var discarded = _framer.DiscardedBytes;
        foreach (var frame in _framer.Push(data))
            Handle(frame, context);

        if (_framer.DiscardedBytes != discarded)
        {
            context.Line.Stats.Discarded += _framer.DiscardedBytes - discarded;
            context.LogReceived([], null, FrameStatus.Noise,
                new FrameInfo("noise", Args(("count", (_framer.DiscardedBytes - discarded).ToString(CultureInfo.InvariantCulture)))));
        }
    }

    private static void Handle(ElenFrame frame, ILineContext context)
    {
        context.Line.Stats.Frames++;
        // tabula ELEN konci spravu prvym 04 - pri 04 v obsahu jej sucet nesedi a spravu odmietne
        var accepted = frame.ChecksumOk && !frame.EotInData;
        var status = !frame.ChecksumOk ? FrameStatus.BadChecksum : frame.EotInData ? FrameStatus.EotInData : FrameStatus.Ok;
        if (!accepted)
            context.Line.Stats.BadFrames++;

        if (frame.IsBroadcast)
        {
            var info = Describe(frame, context.Line.Manufacturer);
            if (accepted)
            {
                foreach (var board in context.Boards)
                {
                    if (board.Display is not ElenDisplay)
                        continue;
                    info = Apply(board, frame, false, context, out _);
                    context.Changed(board);
                }
            }

            context.LogReceived(frame.Raw, 0, status, Broadcast(info));
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

        if (!accepted)
        {
            context.LogReceived(frame.Raw, frame.Address, status, Describe(frame, target.ManufacturerKey));
            context.Reply(target, [ElenFrame.Nak], new FrameInfo("nak"), false);
            return;
        }

        var secondAddress = target.SecondAddress == frame.Address && target.Address != frame.Address;
        var applied = Apply(target, frame, secondAddress, context, out var reply);
        context.Changed(target);
        context.LogReceived(frame.Raw, frame.Address, FrameStatus.Ok, applied);
        if (reply != null)
            context.Reply(target, reply, new FrameInfo("data"));
        else if (frame.Command == CmdReset)
            context.Reply(target, [ResetReply], new FrameInfo("reset"));
        else
            context.Reply(target, [ElenFrame.Ack], new FrameInfo("ack"));
    }

    /// <summary>
    /// Vykona prikaz na tabuli a vrati jeho rozlozeny vyznam.
    /// </summary>
    /// <param name="board">tabula</param>
    /// <param name="frame">sprava</param>
    /// <param name="secondAddress">sprava prisla na druhu adresu panelu - riadky su v hornej polovici</param>
    /// <param name="context">kontext linky</param>
    /// <param name="reply">odpoved s datami (prikazy 2 a 5); null = jednoduche potvrdenie</param>
    private static FrameInfo Apply(SimBoard board, ElenFrame frame, bool secondAddress, ILineContext context, out byte[]? reply)
    {
        reply = null;
        var display = (ElenDisplay)board.Display;
        var variant = ElenVariant.Find(board.ManufacturerKey) ?? ElenVariant.Elen;
        var p = frame.Parameters;
        // druha adresa panelu ovlada hornu polovicu riadkov
        var rowOffset = secondAddress ? board.Profile.Rows / 2 : 0;

        switch (frame.Command)
        {
            case CmdRow when p.Length >= 1:
            {
                var row = p[0] & 0x7F;
                if (row == 0)
                {
                    display.Clear();
                    break;
                }

                // CR v obsahu pokracuje na nizsom riadku
                var content = p.AsSpan(1);
                if (!variant.ExtendedFont && ExtendedFontNumber(content) is { } ext)
                    board.Mismatch = new ManufacturerMismatch("elen.extendedFont", ext > 4 ? ["ELEN16", "ELEN16Kam"] : ["ELEN10", "ELEN16", "ELEN16Kam"]);
                for (var r = row + rowOffset; r > 0; r--)
                {
                    var end = content.IndexOf(CarriageReturn);
                    var part = ElenRowParser.Parse(end < 0 ? content : content[..end], variant);
                    display.SetRow(r, part);
                    if (board.Profile.AutoSize)
                        Grow(board, r, part);
                    if (end < 0)
                        break;
                    content = content[(end + 1)..];
                }

                break;
            }

            case CmdReadRow when p.Length >= 1:
            {
                var row = (p[0] & 0x7F) + rowOffset;
                var raw = display.Rows.TryGetValue(row, out var content) ? content.Raw : [];
                reply = DataReply(frame, [(byte)(0x80 | row), .. raw]);
                break;
            }

            case CmdMoving when p.Length >= 1:
            {
                var part = p[0] & 0x3F;
                var last = (p[0] & 0x40) != 0;
                var data = p.AsSpan(1);
                if (part == 0)
                {
                    display.MovingUpload.Clear();
                    // prva cast: hlavicka (INISS2 meno) az po 03
                    var header = data.IndexOf((byte)0x03);
                    if (header >= 0)
                        data = data[(header + 1)..];
                }

                display.MovingUpload.AddRange(data);
                if (!last)
                    break;

                var codes = MovingText(display.MovingUpload);
                display.MovingCodes = codes;
                display.MovingText = variant.Encoding.GetString(codes);
                display.MovingUpload.Clear();
                return new FrameInfo("elen.moving", Args(("part", Num(part)), ("last", "1"), ("text", display.MovingText)));
            }

            case CmdLight when p.Length >= 2:
            {
                var row = p[0] & 0x7F;
                display.MovingRow = row == 0 ? 0 : row + rowOffset;
                // jas cervenej a zelenej; jeden bajt = obom rovnaky (INISS 3.39)
                display.Brightness = p[1] & 0x7F;
                display.BrightnessGreen = p.Length >= 3 ? p[2] & 0x7F : display.Brightness;
                break;
            }

            case CmdTime when p.Length >= 12:
                if (ParseClock(p) is { } clock)
                {
                    display.ClockValue = clock;
                    display.ClockSetAt = context.Now;
                }

                break;

            case CmdStatus:
                reply = DataReply(frame, [0x80, 0x80]);
                break;

            case CmdTimeout when p.Length >= 1:
                display.Timeout = p[0] & 0x7F;
                break;

            case CmdFlash when p.Length >= 2:
                display.FlashOn = p[0] & 0x7F;
                display.FlashOff = p[1] & 0x7F;
                break;
        }

        return Describe(frame, board.ManufacturerKey);
    }

    /// <summary>
    /// Odpoved s datami: <c>02 adresa|80 prikaz|80 data… 04 sucetH|80 sucetL|80</c>, sucet ako pri sprave od INISSu.
    /// </summary>
    private static byte[] DataReply(ElenFrame frame, ReadOnlySpan<byte> data)
    {
        var reply = new byte[data.Length + 6];
        reply[0] = 0x02;
        reply[1] = frame.Raw[1];
        reply[2] = frame.Raw[2];
        data.CopyTo(reply.AsSpan(3));
        reply[^3] = ElenFrame.Eot;
        (reply[^2], reply[^1]) = ElenChecksum.Encode(ElenChecksum.Compute(reply.AsSpan(0, reply.Length - 2)));
        return reply;
    }

    /// <summary>
    /// Vyznam spravy bez jej vykonania (zaznam komunikacie).
    /// </summary>
    private static FrameInfo Describe(ElenFrame frame, string manufacturer)
    {
        var p = frame.Parameters;
        var variant = ElenVariant.Find(manufacturer) ?? ElenVariant.Elen;
        return frame.Command switch
        {
            CmdRow when p.Length >= 1 && (p[0] & 0x7F) == 0 => new FrameInfo("elen.clear"),
            CmdRow when p.Length >= 1 => new FrameInfo("elen.row",
                Args(("row", Num(p[0] & 0x7F)), ("text", ElenRowParser.Parse(p.AsSpan(1), variant).PlainText))),
            CmdReadRow when p.Length >= 1 => new FrameInfo("elen.readRow", Args(("row", Num(p[0] & 0x7F)))),
            CmdMoving when p.Length >= 1 => new FrameInfo("elen.moving", Args(("part", Num(p[0] & 0x3F)), ("last", "0"), ("text", ""))),
            CmdLight when p.Length >= 2 => new FrameInfo("elen.light", Args(("row", Num(p[0] & 0x7F)), ("brightness", Num(p[1] & 0x7F)))),
            CmdTime when p.Length >= 12 => new FrameInfo("elen.time",
                Args(("time", ParseClock(p)?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? ""))),
            CmdStatus => new FrameInfo("elen.status"),
            CmdTimeout when p.Length >= 1 => new FrameInfo("elen.timeout", Args(("seconds", Num(p[0] & 0x7F)))),
            CmdFlash when p.Length >= 2 => new FrameInfo("elen.flash", Args(("on", Num((p[0] & 0x7F) * 100)), ("off", Num((p[1] & 0x7F) * 100)))),
            CmdReset => new FrameInfo("elen.reset"),
            _ when AckOnly.Contains(frame.Command) => new FrameInfo("elen.cmd", Args(("cmd", Num(frame.Command)))),
            _ => new FrameInfo("elen.unknown", Args(("cmd", Num(frame.Command))))
        };
    }

    private static FrameInfo Broadcast(FrameInfo info)
    {
        var args = new Dictionary<string, string>(info.Args ?? new Dictionary<string, string>()) { ["broadcast"] = "1" };
        return info with { Args = args };
    }

    /// <summary>Text z nahrateho <c>"T0" 1F text " " 1F</c> (INISS2: <c>"I…S4T0" 1F text "  " 1F</c>).</summary>
    private static byte[] MovingText(List<byte> upload)
    {
        var first = upload.IndexOf(0x1F);
        var lastSep = upload.LastIndexOf(0x1F);
        var start = first + 1;
        var end = lastSep > first ? lastSep : upload.Count;
        while (end > start && upload[end - 1] == 0x20)
            end--;
        return upload.GetRange(start, Math.Max(0, end - start)).ToArray();
    }

    /// <summary><c>HHMMSSDDMMRR</c> a den v tyzdni; null pri neplatnom case.</summary>
    private static DateTime? ParseClock(byte[] p)
    {
        Span<int> v = stackalloc int[6];
        for (var i = 0; i < 6; i++)
        {
            int hi = p[2 * i] - '0', lo = p[2 * i + 1] - '0';
            if (hi is < 0 or > 9 || lo is < 0 or > 9)
                return null;
            v[i] = hi * 10 + lo;
        }

        try
        {
            return new DateTime(2000 + v[5], v[4], v[3], v[0], v[1], v[2], DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// Cislo rozsireneho pisma (<c>19 h</c>, h s bitom 0x80), ak ho obsah riadku nesie; inak null. INISS ho posiela len
    /// tabuliam ELEN10, ELEN16 a ELEN16Kam (ELEN10 pozna pisma 1-4) a ako text nikdy neposiela kod pod 0x20.
    /// </summary>
    private static int? ExtendedFontNumber(ReadOnlySpan<byte> content)
    {
        for (var i = 0; i + 1 < content.Length; i++)
            if (content[i] == 0x19 && (content[i + 1] & 0x80) != 0)
                return content[i + 1] & 0x0F;
        return null;
    }

    /// <summary>Tabula zo samoucenia sa zvacsi, aby sa obsah zmestil.</summary>
    private static void Grow(SimBoard board, int row, RowContent content)
    {
        var width = board.Profile.Width;
        foreach (var run in content.Elements.SelectMany(Runs))
        {
            if (run.X is { } x)
                width = Math.Max(width, x + run.Codes.Length * 6);
        }

        width = (width + 31) / 32 * 32;
        if (row > board.Profile.Rows || width > board.Profile.Width)
            board.Profile = board.Profile with { Rows = Math.Max(row, board.Profile.Rows), Width = width };
    }

    private static IEnumerable<TextRun> Runs(RowElement element) => element switch
    {
        RunElement r => [r.Run],
        AlternationElement a => a.Parts.SelectMany(p => p),
        _ => []
    };

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);
}
