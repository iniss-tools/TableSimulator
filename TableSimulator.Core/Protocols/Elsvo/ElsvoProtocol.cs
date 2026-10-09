using System.Globalization;
using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols.Elsvo;

/// <summary>
/// Sprava ELSVO - prikaz INISSu aj odpoved jednotky maju pat bajtov <c>02 adresa prikaz/stav 03 sucet</c>.
/// </summary>
/// <param name="Address">adresa jednotky</param>
/// <param name="Code">prikaz (od INISSu) alebo stav (od jednotky)</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">sucet sedi</param>
public sealed record ElsvoFrame(int Address, byte Code, byte[] Raw, bool ChecksumOk)
{
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;
    public const int Length = 5;

    /// <summary>Zapnut zosilnovac.</summary>
    public const byte On = 0x1F;

    /// <summary>Zapnut zosilnovac v druhom dennom pasme okruhu.</summary>
    public const byte OnBand2 = 0x3F;

    /// <summary>Dotaz na stav.</summary>
    public const byte Query = 0x80;

    /// <summary>Uvolnit po skonceni hlasenia.</summary>
    public const byte Release = 0xC0;

    /// <summary>Vypnut.</summary>
    public const byte Off = 0x00;

    /// <summary>Sucet doplna sucet vsetkych piatich bajtov do nuly (zaporny sucet prvych styroch).</summary>
    public static byte Checksum(int address, byte code) => (byte)-(Stx + address + code + Etx);

    /// <summary>Sprava s adresou a prikazom alebo stavom.</summary>
    public static byte[] Build(int address, byte code) => [Stx, (byte)address, code, Etx, Checksum(address, code)];
}

/// <summary>
/// Sklada spravy ELSVO z prudu bajtov: <c>02</c>, adresa, kod, <c>03</c>, sucet. Bajty mimo sprav zahodi.
/// </summary>
public sealed class ElsvoFramer
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

    /// <summary>Prida prijate bajty a vrati spravy, ktore sa nimi dokoncili (aj so zlym suctom).</summary>
    public IReadOnlyList<ElsvoFrame> Push(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            _buffer.Add(b);

        List<ElsvoFrame>? frames = null;
        while (_buffer.Count > 0)
        {
            if (_buffer[0] != ElsvoFrame.Stx || (_buffer.Count > 3 && _buffer[3] != ElsvoFrame.Etx))
            {
                _buffer.RemoveAt(0);
                DiscardedBytes++;
                continue;
            }

            if (_buffer.Count < ElsvoFrame.Length)
                break;
            var raw = _buffer.GetRange(0, ElsvoFrame.Length).ToArray();
            _buffer.RemoveRange(0, ElsvoFrame.Length);
            (frames ??= []).Add(new ElsvoFrame(raw[1], raw[2], raw, ElsvoFrame.Checksum(raw[1], raw[2]) == raw[4]));
        }

        return frames ?? (IReadOnlyList<ElsvoFrame>)[];
    }
}

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
