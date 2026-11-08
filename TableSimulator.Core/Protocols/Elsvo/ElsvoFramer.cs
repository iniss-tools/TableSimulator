namespace TableSimulator.Core.Protocols.Elsvo;

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
