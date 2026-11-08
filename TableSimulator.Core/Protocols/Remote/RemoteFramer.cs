namespace TableSimulator.Core.Protocols.Remote;

/// <summary>
/// Sklada spravy riadiaceho INISSu z prudu bajtov podla dlzky v hlavicke; bajty mimo spravy (iny typ, nezmyselna
/// dlzka) zahodi po jednom.
/// </summary>
public sealed class RemoteFramer
{
    private readonly List<byte> _buffer = [];

    /// <summary>Pocet bajtov, ktore sa nedali zaradit do spravy.</summary>
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
    public IReadOnlyList<RemoteMessage> Push(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            _buffer.Add(b);

        List<RemoteMessage>? messages = null;
        while (_buffer.Count > 0)
        {
            if (_buffer[0] != RemoteMessage.Request)
            {
                _buffer.RemoveAt(0);
                DiscardedBytes++;
                continue;
            }

            if (_buffer.Count < 5)
                break;
            var length = _buffer[1] | _buffer[2] << 8 | _buffer[3] << 16 | _buffer[4] << 24;
            if (length is < RemoteMessage.MinLength or > RemoteMessage.MaxLength)
            {
                _buffer.RemoveAt(0);
                DiscardedBytes++;
                continue;
            }

            if (_buffer.Count < length)
                break;
            var raw = _buffer.GetRange(0, length).ToArray();
            _buffer.RemoveRange(0, length);
            (messages ??= []).Add(RemoteMessage.Parse(raw));
        }

        return messages ?? (IReadOnlyList<RemoteMessage>)[];
    }
}
