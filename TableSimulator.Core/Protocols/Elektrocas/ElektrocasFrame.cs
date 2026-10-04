namespace TableSimulator.Core.Protocols.Elektrocas;

/// <summary>
/// Sprava protokolu Elektrocas (LCD, LCD1, ERP): <c>adresa dlzka odosielatel prikaz parametre… sucet</c>, bez
/// zaciatocnych a koncovych znakov - hranicu urcuje dlzka. Sucet = dolnych 8 bitov suctu vsetkych predchadzajucich bajtov.
/// </summary>
/// <param name="Address">prijemca; <c>FF</c> = vsetky tabule</param>
/// <param name="Sender">odosielatel (od INISSu <c>ComputerAddr</c>)</param>
/// <param name="Command">prikaz</param>
/// <param name="Parameters">parametre</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">sucet sedi</param>
public sealed record ElektrocasFrame(int Address, int Sender, int Command, byte[] Parameters, byte[] Raw, bool ChecksumOk)
{
    /// <summary>Adresa vsetkych tabul na linke.</summary>
    public const int Broadcast = 0xFF;

    /// <summary>Najkratsia sprava (bez parametrov).</summary>
    public const int MinLength = 5;

    /// <summary>Sucet bajtov.</summary>
    public static byte Checksum(ReadOnlySpan<byte> bytes)
    {
        var sum = 0;
        foreach (var b in bytes)
            sum += b;
        return (byte)sum;
    }

    /// <summary>Zostavi spravu s dlzkou a suctom.</summary>
    public static byte[] Build(int address, int sender, int command, ReadOnlySpan<byte> parameters)
    {
        var raw = new byte[parameters.Length + MinLength];
        raw[0] = (byte)address;
        raw[1] = (byte)raw.Length;
        raw[2] = (byte)sender;
        raw[3] = (byte)command;
        parameters.CopyTo(raw.AsSpan(4));
        raw[^1] = Checksum(raw.AsSpan(0, raw.Length - 1));
        return raw;
    }
}

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
