namespace TableSimulator.Core.Protocols.Elen;

/// <summary>
/// Kontrolny sucet ELEN - dolny bajt CRC-16/ARC zo vsetkych bajtov od <c>01</c> po <c>04</c> vratane.
/// </summary>
public static class ElenChecksum
{
    /// <summary>Kontrolny sucet bajtov <paramref name="frame" /> (od SOH po EOT vratane).</summary>
    public static byte Compute(ReadOnlySpan<byte> frame)
    {
        ushort crc = 0;
        foreach (var b in frame)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
        }

        return (byte)crc;
    }

    /// <summary>Sucet rozdeleny na dva bajty s hornym bitom (tak, ako sa posiela).</summary>
    public static (byte High, byte Low) Encode(byte sum) => ((byte)(0x80 | (sum >> 4)), (byte)(0x80 | (sum & 0x0F)));
}

/// <summary>
/// Sprava od INISSu: <c>01 adresa|80 prikaz|80 parametre… 04 sucetH|80 sucetL|80</c>.
/// </summary>
/// <param name="Address">adresa tabule 0-127; 0 = hromadna sprava</param>
/// <param name="Command">cislo prikazu</param>
/// <param name="Parameters">parametre medzi prikazom a EOT</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">kontrolny sucet sedi</param>
public sealed record ElenFrame(int Address, int Command, byte[] Parameters, byte[] Raw, bool ChecksumOk)
{
    public const byte Soh = 0x01;
    public const byte Eot = 0x04;
    public const byte Ack = 0x06;
    public const byte Nak = 0x15;

    /// <summary>Hromadna sprava pre vsetky tabule na linke.</summary>
    public bool IsBroadcast => Address == 0;

    /// <summary>
    /// Zostavi spravu (na testy a nastroje): doplni rámec a kontrolny sucet.
    /// </summary>
    public static byte[] Build(int address, int command, ReadOnlySpan<byte> parameters)
    {
        var raw = new byte[parameters.Length + 6];
        raw[0] = Soh;
        raw[1] = (byte)(0x80 | address);
        raw[2] = (byte)(0x80 | command);
        parameters.CopyTo(raw.AsSpan(3));
        raw[^3] = Eot;
        (raw[^2], raw[^1]) = ElenChecksum.Encode(ElenChecksum.Compute(raw.AsSpan(0, raw.Length - 2)));
        return raw;
    }
}

/// <summary>
/// Sklada spravy ELEN z prudu bajtov - spravy mozu prist po castiach aj viac naraz.
/// </summary>
public sealed class ElenFramer
{
    private const int MaxFrameLength = 4096;

    private readonly List<byte> _buffer = [];
    private int _checksumBytes = -1;

    /// <summary>Pocet bajtov mimo sprav (sum na linke), ktore sa zahodili.</summary>
    public long DiscardedBytes { get; private set; }

    /// <summary>Zahodi rozpracovanu spravu (nove spojenie).</summary>
    public void Reset()
    {
        _buffer.Clear();
        _checksumBytes = -1;
    }

    /// <summary>
    /// Prida prijate bajty a vrati spravy, ktore sa nimi dokoncili.
    /// </summary>
    public IReadOnlyList<ElenFrame> Push(ReadOnlySpan<byte> data)
    {
        List<ElenFrame>? frames = null;
        foreach (var b in data)
        {
            if (_buffer.Count == 0)
            {
                if (b == ElenFrame.Soh)
                    _buffer.Add(b);
                else
                    DiscardedBytes++;
                continue;
            }

            if (_checksumBytes >= 0)
            {
                _buffer.Add(b);
                if (++_checksumBytes == 2)
                {
                    (frames ??= []).Add(Complete());
                    Reset();
                }

                continue;
            }

            if (b == ElenFrame.Soh)
            {
                // novy zaciatok uprostred spravy - predchadzajuca sa nedokoncila
                DiscardedBytes += _buffer.Count;
                _buffer.Clear();
                _buffer.Add(b);
                continue;
            }

            _buffer.Add(b);
            if (b == ElenFrame.Eot && _buffer.Count >= 3)
                _checksumBytes = 0;
            else if (_buffer.Count > MaxFrameLength)
            {
                DiscardedBytes += _buffer.Count;
                Reset();
            }
        }

        return frames ?? (IReadOnlyList<ElenFrame>)[];
    }

    private ElenFrame Complete()
    {
        var raw = _buffer.ToArray();
        var sum = ElenChecksum.Compute(raw.AsSpan(0, raw.Length - 2));
        var (high, low) = ElenChecksum.Encode(sum);
        var ok = raw[^2] == high && raw[^1] == low;
        var parameters = raw.Length > 6 ? raw[3..^3] : [];
        return new ElenFrame(raw[1] & 0x7F, raw.Length > 5 ? raw[2] & 0x7F : -1, parameters, raw, ok);
    }
}
