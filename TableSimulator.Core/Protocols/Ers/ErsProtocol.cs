using System.Globalization;
using System.Text;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.Flaps;

namespace TableSimulator.Core.Protocols.Ers;

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
