namespace TableSimulator.Core.Protocols.Elen;

/// <summary>
/// Sklada spravy ELEN z prudu bajtov - spravy mozu prist po castiach aj viac naraz. Za koncom spravy (<c>04</c>)
/// idu dva bajty suctu, oba s hornym bitom; ak nasleduje iny bajt, <c>04</c> bolo v obsahu a sprava pokracuje
/// (<see cref="ElenFrame.EotInData" />).
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
                if ((b & 0x80) != 0)
                {
                    _buffer.Add(b);
                    if (++_checksumBytes == 2)
                    {
                        (frames ??= []).Add(Complete());
                        Reset();
                    }

                    continue;
                }

                // bajt bez horneho bitu nemoze byt sucet - 04 aj doterajsie bajty patria do obsahu
                _checksumBytes = -1;
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
