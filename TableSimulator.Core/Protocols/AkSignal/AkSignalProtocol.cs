using System.Globalization;
using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols.AkSignal;

/// <summary>
/// Prikaz servera INISS2 ustredni AK Signal: <c>adresa dlzka prikaz [hodnota hodnota] sucet</c>, dlzka 4 alebo 6 (prikaz
/// <see cref="SetAudio" /> s hodnotou dolnym bajtom napred).
/// </summary>
/// <param name="Address">adresa ustredne 1-255</param>
/// <param name="Command">prikaz</param>
/// <param name="Value">hodnota prikazu s dlzkou 6; null = prikaz bez hodnoty</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">sucet sedi</param>
public sealed record AkSignalFrame(int Address, byte Command, int? Value, byte[] Raw, bool ChecksumOk)
{
    /// <summary>Dotaz na stav rozhlasu.</summary>
    public const byte GetAudio = 0x10;

    /// <summary>Vypnut rozhlas.</summary>
    public const byte ClearAudio = 0x11;

    /// <summary>Zapnut rozhlas s dvojbajtovou hodnotou.</summary>
    public const byte SetAudio = 0x12;

    /// <summary>Dotaz na stav svetla.</summary>
    public const byte GetLight = 0x20;

    /// <summary>Vypnut automaticke svietenie.</summary>
    public const byte ClearLight = 0x21;

    /// <summary>Zapnut automaticke svietenie.</summary>
    public const byte SetLight = 0x22;

    /// <summary>Zapnut svetlo rucne.</summary>
    public const byte SetManual = 0x23;

    /// <summary>Zrusit rucne svietenie.</summary>
    public const byte ClearManual = 0x24;

    /// <summary>Hodnota zapnutia rozhlasu bez <c>daymode</c>/<c>nightmode</c> v Audio.xml.</summary>
    public const int NoValue = 0xFFFF;

    /// <summary>Dlzka odpovede ustredne.</summary>
    public const int AnswerLength = 5;

    /// <summary>Sucet: dolnych 8 bitov suctu bajtov.</summary>
    public static byte Checksum(ReadOnlySpan<byte> bytes)
    {
        var sum = 0;
        foreach (var b in bytes)
            sum += b;
        return (byte)sum;
    }

    /// <summary>Prikaz servera - pri <see cref="SetAudio" /> s hodnotou <paramref name="value" />.</summary>
    public static byte[] Build(int address, byte command, int value = NoValue)
    {
        byte[] frame = command == SetAudio
            ? [(byte)address, 6, command, (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF), 0]
            : [(byte)address, 4, command, 0];
        frame[^1] = Checksum(frame.AsSpan(0, frame.Length - 1));
        return frame;
    }

    /// <summary>Odpoved ustredne: <c>00 05 prikaz stav sucet</c> - adresu nenesie.</summary>
    public static byte[] Answer(byte command, byte state)
    {
        byte[] frame = [0x00, AnswerLength, command, state, 0];
        frame[^1] = Checksum(frame.AsSpan(0, AnswerLength - 1));
        return frame;
    }

    /// <summary>Prikaz pre svetlo (inak pre rozhlas).</summary>
    public static bool IsLight(byte command) => command is >= GetLight and <= ClearManual;
}

/// <summary>
/// Sklada prikazy AK Signal z prudu bajtov podla dlzky v druhom bajte (4 alebo 6). Bajty, ktore nezacinaju prikaz
/// s platnou dlzkou, zahodi.
/// </summary>
public sealed class AkSignalFramer
{
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

    /// <summary>Prida prijate bajty a vrati prikazy, ktore sa nimi dokoncili (aj so zlym suctom).</summary>
    public IReadOnlyList<AkSignalFrame> Push(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            _buffer.Add(b);

        List<AkSignalFrame>? frames = null;
        while (_buffer.Count >= 2)
        {
            var length = _buffer[1];
            if (length is not (4 or 6))
            {
                _buffer.RemoveAt(0);
                DiscardedBytes++;
                continue;
            }

            if (_buffer.Count < length)
                break;
            var raw = _buffer.GetRange(0, length).ToArray();
            _buffer.RemoveRange(0, length);
            int? value = length == 6 ? raw[3] | raw[4] << 8 : null;
            (frames ??= []).Add(new AkSignalFrame(raw[0], raw[2], value, raw, AkSignalFrame.Checksum(raw.AsSpan(0, length - 1)) == raw[^1]));
        }

        return frames ?? (IReadOnlyList<AkSignalFrame>)[];
    }
}

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
