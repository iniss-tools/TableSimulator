using System.Globalization;
using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols.Elen;

/// <summary>
/// Protokol ELEN (TableClass 4) z pohladu tabule: potvrdzuje spravy jednym bajtom a pamata si obsah riadkov.
/// </summary>
internal sealed class ElenProtocol : ILineProtocol
{
    private const int CmdTime = 0;
    private const int CmdRow = 1;
    private const int CmdMoving = 3;
    private const int CmdLight = 4;
    private const int CmdStatus = 5;

    private readonly ElenFramer _framer = new();

    public void Reset() => _framer.Reset();

    public BoardDisplay CreateDisplay() => new ElenDisplay();

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
        if (!frame.ChecksumOk)
            context.Line.Stats.BadFrames++;

        if (frame.IsBroadcast)
        {
            var info = Describe(frame, context.Line.Manufacturer);
            if (frame.ChecksumOk)
            {
                foreach (var board in context.Boards)
                {
                    if (board.Display is not ElenDisplay)
                        continue;
                    info = Apply(board, frame, context);
                    context.Changed(board);
                }
            }

            context.LogReceived(frame.Raw, 0, frame.ChecksumOk ? FrameStatus.Ok : FrameStatus.BadChecksum, Broadcast(info));
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
            context.Reply(target, [ElenFrame.Nak], new FrameInfo("nak"));
            return;
        }

        var applied = Apply(target, frame, context);
        context.Changed(target);
        context.LogReceived(frame.Raw, frame.Address, FrameStatus.Ok, applied);
        context.Reply(target, [ElenFrame.Ack], new FrameInfo("ack"));
    }

    /// <summary>
    /// Vykona prikaz na tabuli a vrati jeho rozlozeny vyznam.
    /// </summary>
    private static FrameInfo Apply(SimBoard board, ElenFrame frame, ILineContext context)
    {
        var display = (ElenDisplay)board.Display;
        var variant = ElenVariant.Find(board.ManufacturerKey) ?? ElenVariant.Elen;
        var p = frame.Parameters;

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

                var content = ElenRowParser.Parse(p.AsSpan(1), variant);
                display.SetRow(row, content);
                if (board.Profile.AutoSize)
                    Grow(board, row, content);
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
                display.MovingRow = p[0] & 0x7F;
                display.Brightness = Brightness(p);
                break;

            case CmdTime when p.Length >= 12:
                if (ParseClock(p) is { } clock)
                {
                    display.ClockValue = clock;
                    display.ClockSetAt = context.Now;
                }

                break;
        }

        return Describe(frame, board.ManufacturerKey);
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
            CmdMoving when p.Length >= 1 => new FrameInfo("elen.moving", Args(("part", Num(p[0] & 0x3F)), ("last", "0"), ("text", ""))),
            CmdLight when p.Length >= 2 => new FrameInfo("elen.light", Args(("row", Num(p[0] & 0x7F)), ("brightness", Num(Brightness(p))))),
            CmdTime when p.Length >= 12 => new FrameInfo("elen.time",
                Args(("time", ParseClock(p)?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? ""))),
            CmdStatus => new FrameInfo("elen.status"),
            _ => new FrameInfo("elen.unknown", Args(("cmd", Num(frame.Command))))
        };
    }

    /// <summary>Jas z prikazu 4; INISS2 posiela pri ELEN10/16 jas v dvoch bajtoch (horny a dolny).</summary>
    private static int Brightness(byte[] p) => p.Length >= 3 ? ((p[1] & 0x7F) << 7) | (p[2] & 0x7F) : p[1] & 0x7F;

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
