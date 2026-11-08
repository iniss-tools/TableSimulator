using System.Text;

namespace TableSimulator.Core.Protocols.Ers;

/// <summary>
/// Sklada spravy ERS z prudu bajtov: zaciatok <c>16 16 01</c>, za adresou bud <c>05</c> (dotaz), alebo text od
/// <c>02</c> po <c>03</c> a BCC. Bajty mimo sprav zahodi.
/// </summary>
public sealed class ErsFramer
{
    private const int MaxFrameLength = 512;

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
    public IReadOnlyList<ErsFrame> Push(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            _buffer.Add(b);

        List<ErsFrame>? frames = null;
        while (_buffer.Count > 0)
        {
            // zaciatok 16 16 01 - kym sa nezhoduje, prvy bajt je sum
            var prefix = Math.Min(3, _buffer.Count);
            if (!_buffer.Take(prefix).SequenceEqual(new byte[] { ErsFrame.Syn, ErsFrame.Syn, ErsFrame.Soh }.Take(prefix)))
            {
                Discard();
                continue;
            }

            if (_buffer.Count < 5)
                break;
            if (_buffer[4] == ErsFrame.Enq)
            {
                var raw = Take(5);
                (frames ??= []).Add(new ErsFrame(raw[3], null, [], raw, true));
                continue;
            }

            if (_buffer[4] != ErsFrame.Stx)
            {
                Discard();
                continue;
            }

            var etx = _buffer.IndexOf(ErsFrame.Etx, 5);
            if (etx < 0)
            {
                if (_buffer.Count > MaxFrameLength)
                    Discard();
                else
                    break;
                continue;
            }

            if (_buffer.Count < etx + 2)
                break;
            var frame = Take(etx + 2);
            var body = frame.AsSpan(5, etx - 5);
            var ok = ErsFrame.Bcc(frame.AsSpan(4, etx - 3)) == (frame[^1] & 0x7F);
            var command = body.Length >= 2 ? Encoding.ASCII.GetString(body[..2]) : "";
            (frames ??= []).Add(new ErsFrame(frame[3], command, body.Length >= 2 ? body[2..].ToArray() : [], frame, ok));
        }

        return frames ?? (IReadOnlyList<ErsFrame>)[];
    }

    private void Discard()
    {
        _buffer.RemoveAt(0);
        DiscardedBytes++;
    }

    private byte[] Take(int count)
    {
        var raw = _buffer.GetRange(0, count).ToArray();
        _buffer.RemoveRange(0, count);
        return raw;
    }
}
