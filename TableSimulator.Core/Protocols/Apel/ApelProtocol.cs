using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.Chars;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Protocols.Apel;

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
