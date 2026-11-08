namespace TableSimulator.Core.Protocols.Elektrocas;

/// <summary>
/// Sklada spravy Elektrocas z prudu bajtov podla dlzky v druhom bajte.
/// </summary>
public sealed class ElektrocasFramer
{
    private readonly List<byte> _buffer = [];

    /// <summary>Pocet bajtov, ktore sa nedali zaradit do spravy (zla dlzka).</summary>
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
    public IReadOnlyList<ElektrocasFrame> Push(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            _buffer.Add(b);

        List<ElektrocasFrame>? frames = null;
        while (_buffer.Count >= 2)
        {
            int length = _buffer[1];
            if (length < ElektrocasFrame.MinLength)
            {
                // dlzka nedava zmysel - prvy bajt je sum
                _buffer.RemoveAt(0);
                DiscardedBytes++;
                continue;
            }

            if (_buffer.Count < length)
                break;

            var raw = _buffer.GetRange(0, length).ToArray();
            _buffer.RemoveRange(0, length);
            var ok = ElektrocasFrame.Checksum(raw.AsSpan(0, length - 1)) == raw[^1];
            (frames ??= []).Add(new ElektrocasFrame(raw[0], raw[2], raw[3], raw[4..^1], raw, ok));
        }

        return frames ?? (IReadOnlyList<ElektrocasFrame>)[];
    }
}
