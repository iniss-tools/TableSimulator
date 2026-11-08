using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;

namespace TableSimulator.Core.Protocols.AkSignal;

/// <summary>
/// Ustredna AK Signal s protokolom AKSIGNAL (TableClass 131 simulatora, INISS 3.39 ju nepozna): na adrese ustredne
/// svetlo aj rozhlas. Prikazy vykona a na kazdy odpovie stavom svetla alebo zvuku. Zly sucet ustredna nevykona
/// a neodpovie - to je predpoklad, server neodpoved zopakuje. Viz docs/iniss2/protokoly/aksignal.mdx.
/// </summary>
internal sealed class AkSignalProtocol : ILineProtocol
{
    /// <summary>Kluc vyrobcu ustredne v simulatore.</summary>
    public const string Manufacturer = "AKSIGNAL";

    private static readonly TimeSpan ReceiveGap = TimeSpan.FromMilliseconds(500);

    private static readonly (int Bit, string Name)[] AudioBits =
        [(AkSignalDisplay.AudioPlaying, "ON"), (AkSignalDisplay.AudioHighPriority, "HIPRT"), (AkSignalDisplay.AudioCancelled, "CANCEL"),
            (AkSignalDisplay.AudioOutputError, "OUTERR"), (AkSignalDisplay.AudioAmpPower, "PWROK")];

    private static readonly (int Bit, string Name)[] LightBits =
        [(AkSignalDisplay.LightAuto, "AUTO"), (AkSignalDisplay.LightManual, "MAN"), (AkSignalDisplay.LightDark, "DARK"), (AkSignalDisplay.LightOk, "OK")];

    private readonly AkSignalFramer _framer = new();
    private DateTimeOffset _lastData;

    public void Reset() => _framer.Reset();

    public BoardDisplay CreateDisplay(string manufacturer) => new AkSignalDisplay();

    /// <summary>Pri poruche ustredna odpovie nezmyslom, odpovedou so zlym suctom alebo zvolenym stavom.</summary>
    public (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal)
    {
        var command = normal.Length > 2 ? normal[2] : (byte)0;
        switch (fault.Kind)
        {
            case FaultKind.Garbage:
                return ([0x00], new FrameInfo("garbage"));
            case FaultKind.Status:
                return (AkSignalFrame.Answer(command, (byte)fault.Status), StateInfo(command, (byte)fault.Status));
            default:
                var corrupt = normal.ToArray();
                if (corrupt.Length > 0)
                    corrupt[^1] ^= 0xFF;
                return (corrupt, new FrameInfo("aksignal.corrupt"));
        }
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
            context.LogReceived([], null, FrameStatus.Noise, new FrameInfo("noise", Args(("count", Num(_framer.DiscardedBytes - discarded)))));
        }
    }

    private static void Handle(AkSignalFrame frame, ILineContext context)
    {
        context.Line.Stats.Frames++;
        if (!frame.ChecksumOk)
            context.Line.Stats.BadFrames++;

        var target = context.FindBoard(frame.Address);
        if (target?.Display is not AkSignalDisplay display)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.NoBoard, Describe(frame));
            return;
        }

        target.Stats.Frames++;
        target.Stats.LastFrameAt = context.Now;
        if (!frame.ChecksumOk)
        {
            context.LogReceived(frame.Raw, frame.Address, FrameStatus.BadChecksum, Describe(frame));
            return;
        }

        var now = context.Now;
        var profile = target.EffectiveProfile;
        var changed = display.Advance(now, profile, context.Environment.Location);
        var known = true;
        switch (frame.Command)
        {
            case AkSignalFrame.GetAudio:
                changed |= display.QueryAudio(now);
                break;
            case AkSignalFrame.ClearAudio:
                changed |= display.QueryAudio(now) | display.AudioOff(now);
                break;
            case AkSignalFrame.SetAudio:
                changed |= display.QueryAudio(now) | display.AudioOn(frame.Value ?? AkSignalFrame.NoValue, now, profile);
                break;
            case AkSignalFrame.GetLight:
                changed |= display.QueryLight(now);
                break;
            case AkSignalFrame.ClearLight or AkSignalFrame.SetLight:
                changed |= display.QueryLight(now) | display.SetAuto(frame.Command == AkSignalFrame.SetLight, now);
                break;
            case AkSignalFrame.SetManual or AkSignalFrame.ClearManual:
                changed |= display.QueryLight(now) | display.SetManual(frame.Command == AkSignalFrame.SetManual, now);
                break;
            default:
                known = false;
                break;
        }

        if (changed)
            context.Changed(target);
        context.LogReceived(frame.Raw, frame.Address, FrameStatus.Ok, Describe(frame));
        if (!known)
            return;

        var state = AkSignalFrame.IsLight(frame.Command) ? display.LightState(profile) : display.AudioState(profile);
        context.Reply(target, AkSignalFrame.Answer(frame.Command, state), StateInfo(frame.Command, state));
    }

    /// <summary>Vyznam prikazu - kluce zdiela s protokolom AKSIGNALUH.</summary>
    private static FrameInfo Describe(AkSignalFrame frame) => frame.Command switch
    {
        AkSignalFrame.GetAudio => new FrameInfo("aksignal.getAudio"),
        AkSignalFrame.ClearAudio => new FrameInfo("aksignal.clearAudio"),
        AkSignalFrame.SetAudio => new FrameInfo("aksignal.setAudio", Args(("value", Value(frame.Value ?? AkSignalFrame.NoValue)))),
        AkSignalFrame.GetLight => new FrameInfo("aksignal.getLight"),
        AkSignalFrame.ClearLight => new FrameInfo("aksignal.clearLight"),
        AkSignalFrame.SetLight => new FrameInfo("aksignal.setLight"),
        AkSignalFrame.SetManual => new FrameInfo("aksignal.setManual"),
        AkSignalFrame.ClearManual => new FrameInfo("aksignal.clearManual"),
        _ => new FrameInfo("aksignal.unknown", Args(("cmd", frame.Command.ToString("X2", CultureInfo.InvariantCulture))))
    };

    /// <summary>Odpoved ustredne: stav hex a mena bitov (technicke nazvy, nie text rozhrania).</summary>
    private static FrameInfo StateInfo(byte command, byte state)
    {
        var bits = AkSignalFrame.IsLight(command) ? LightBits : AudioBits;
        return new FrameInfo(AkSignalFrame.IsLight(command) ? "aksignal.lightState" : "aksignal.audioState",
            Args(("state", state.ToString("X2", CultureInfo.InvariantCulture)), ("bits", string.Join(" ", bits.Where(b => (state & b.Bit) != 0).Select(b => b.Name)))));
    }

    private static string Value(int value) => value == AkSignalFrame.NoValue ? "FFFF" : Num(value);

    private static string Num(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Args(params (string Key, string Value)[] args) => args.ToDictionary(a => a.Key, a => a.Value);
}
