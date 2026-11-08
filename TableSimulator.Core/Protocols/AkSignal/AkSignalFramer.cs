namespace TableSimulator.Core.Protocols.AkSignal;

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
