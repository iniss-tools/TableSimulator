namespace TableSimulator.Core.Protocols.Elektrocas;

/// <summary>
/// Sklada pakety ITP z prudu bajtov: hlada zaciatok <c>F1 8F</c> a cita podla dlzky.
/// </summary>
public sealed class ItpFramer
{
    private readonly List<byte> _buffer = [];

    /// <summary>Pocet bajtov mimo paketov.</summary>
    public long DiscardedBytes { get; private set; }

    /// <summary>Pocet bajtov rozpracovaneho paketu.</summary>
    public int Pending => _buffer.Count;

    /// <summary>Zahodi rozpracovany paket.</summary>
    public void Reset() => _buffer.Clear();

    /// <summary>Zahodi rozpracovany paket ako sum (po medzere v prijme).</summary>
    public void DropPending()
    {
        DiscardedBytes += _buffer.Count;
        _buffer.Clear();
    }

    /// <summary>Prida prijate bajty a vrati pakety, ktore sa nimi dokoncili (aj so zlym suctom).</summary>
    public IReadOnlyList<ItpPacket> Push(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            _buffer.Add(b);

        List<ItpPacket>? packets = null;
        while (_buffer.Count >= 4)
        {
            if (_buffer[0] != ItpPacket.StartLow || _buffer[1] != ItpPacket.Start >> 8)
            {
                Skip();
                continue;
            }

            var bytes = (_buffer[2] | _buffer[3] << 8) * 2;
            if (bytes < ItpPacket.MinWords * 2 || bytes > ItpPacket.MaxBytes)
            {
                Skip();
                continue;
            }

            if (_buffer.Count < bytes)
                break;

            var raw = _buffer.GetRange(0, bytes).ToArray();
            _buffer.RemoveRange(0, bytes);
            var words = bytes / 2;
            var ok = ItpPacket.Word(raw, words - 1) == ItpPacket.End && ItpPacket.Word(raw, words - 2) == ItpPacket.Sum(raw, words);
            (packets ??= []).Add(new ItpPacket(ItpPacket.Word(raw, 2), ItpPacket.Word(raw, 3), raw[8..^4], raw, ok));
        }

        return packets ?? (IReadOnlyList<ItpPacket>)[];
    }

    /// <summary>Zahodi bajty po dalsi mozny zaciatok paketu.</summary>
    private void Skip()
    {
        var next = 1;
        while (next < _buffer.Count && _buffer[next] != ItpPacket.StartLow)
            next++;
        _buffer.RemoveRange(0, next);
        DiscardedBytes += next;
    }
}
