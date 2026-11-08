using System.Globalization;
using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols.Elsvo;

/// <summary>
/// Spinacie jednotky zosilnovacov ELSVO (TableClass 129) z pohladu jednotky: prikaz zapnut (1F, 3F), uvolnit (C0) alebo
/// vypnut (00) vykona a na kazdy prikaz aj dotaz (80) odpovie stavom - pripravena (80) a pri zopnuti bit hlasenia (10).
/// Zly sucet jednotka nevykona a odpovie bitom chyby prenosu (20) - to je predpoklad, INISS taku odpoved berie ako
/// neuspech. Viz docs/iniss/protokoly/elsvo.mdx.
/// </summary>
internal sealed class ElsvoProtocol : ILineProtocol
{
    private static readonly TimeSpan ReceiveGap = TimeSpan.FromMilliseconds(500);

    private readonly ElsvoFramer _framer = new();
    private DateTimeOffset _lastData;

    public void Reset() => _framer.Reset();

    public BoardDisplay CreateDisplay(string manufacturer) => new AmplifierDisplay();

    /// <summary>
    /// Pri poruche jednotka odpovie nezmyslom, hlasi chybu prenosu (NAK) alebo zvoleny stav - napr. 80 (pripravena,
    /// ale nezopnuta) alebo 84 (chyba hardveru).
    /// </summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal)
    {
        var address = normal.Length > 1 ? normal[1] : 0;
        var state = normal.Length > 2 ? normal[2] : (byte)AmplifierDisplay.Ready;
        return fault.Kind switch
        {
            FaultKind.Garbage => ([0x00], new FrameInfo("garbage")),
            FaultKind.Status => (ElsvoFrame.Build(address, (byte)fault.Status), StateInfo((byte)fault.Status)),
            _ => (ElsvoFrame.Build(address, (byte)(state | AmplifierDisplay.TransferError)), StateInfo((byte)(state | AmplifierDisplay.TransferError)))
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

    private static void Handle(ElsvoFrame frame, ILineContext context)
    {
        context.Line.Stats.Frames++;
        if (!frame.ChecksumOk)
            context.Line.Stats.BadFrames++;

        var target = context.FindBoard(frame.Address);
        if (target?.Display is not AmplifierDisplay display)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.NoBoard, Describe(frame));
            return;
        }

        target.Stats.Frames++;
        target.Stats.LastFrameAt = context.Now;
        if (!frame.ChecksumOk)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.BadChecksum, Describe(frame));
            var error = (byte)(display.State | AmplifierDisplay.TransferError);
            context.Reply(target, ElsvoFrame.Build(frame.Address, error), StateInfo(error), false);
            return;
        }

        var changed = frame.Code switch
        {
            ElsvoFrame.On => display.SwitchOn(frame.Code, 1, context.Now),
            ElsvoFrame.OnBand2 => display.SwitchOn(frame.Code, 2, context.Now),
            ElsvoFrame.Release or ElsvoFrame.Off => display.SwitchOff(frame.Code),
            _ => false
        };
        if (changed)
            context.Changed(target);
        context.LogReceived(frame.Raw, frame.Address, FrameStatus.Ok, Describe(frame));
        context.Reply(target, ElsvoFrame.Build(frame.Address, display.State), StateInfo(display.State));
    }

    private static FrameInfo Describe(ElsvoFrame frame) => frame.Code switch
    {
        ElsvoFrame.On => new FrameInfo("elsvo.on"),
        ElsvoFrame.OnBand2 => new FrameInfo("elsvo.onBand2"),
        ElsvoFrame.Query => new FrameInfo("elsvo.query"),
        ElsvoFrame.Release => new FrameInfo("elsvo.release"),
        ElsvoFrame.Off => new FrameInfo("elsvo.off"),
        _ => new FrameInfo("elsvo.unknown", Args(("cmd", frame.Code.ToString("X2", CultureInfo.InvariantCulture))))
    };

    /// <summary>Odpoved jednotky: stav hex a mena bitov ako v logu INISSu.</summary>
    private static FrameInfo StateInfo(byte state) =>
        new("elsvo.state", Args(("state", state.ToString("X2", CultureInfo.InvariantCulture)), ("bits", StateBits(state))));

    private static readonly (int Bit, string Name)[] BitNames =
        [(0x80, "Ready"), (0x40, "40"), (0x20, "TransferErr"), (0x10, "Hlaseni"), (0x08, "Break"), (0x04, "HwErr"), (0x02, "OP5"), (0x01, "Tlf")];

    /// <summary>Mena nastavenych bitov stavu (nazvy z logu INISSu, nie text rozhrania).</summary>
    public static string StateBits(byte state) => string.Join(" ", BitNames.Where(b => (state & b.Bit) != 0).Select(b => b.Name));

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);
}
