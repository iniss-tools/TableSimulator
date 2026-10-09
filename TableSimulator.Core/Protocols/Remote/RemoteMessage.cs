using System.Buffers.Binary;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Protocols.Remote;

/// <summary>
/// Kontrolny sucet sprav vzdialeneho INISSu: zvysok delenia polynomom CCITT <c>0x1021</c> bez nasobenia spravy
/// <c>x^16</c> (zaciatok 0, bez zrkadlenia a zaverecneho XOR) - nie je to CRC-16/XMODEM.
/// </summary>
public static class RemoteCrc
{
    private static readonly ushort[] Table = CreateTable();

    /// <summary>Sucet bajtov <paramref name="data" />.</summary>
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;
        foreach (var b in data)
            crc = (ushort)(((crc << 8) | b) ^ Table[crc >> 8]);
        return crc;
    }

    private static ushort[] CreateTable()
    {
        var table = new ushort[256];
        for (var i = 0; i < 256; i++)
        {
            var crc = (ushort)(i << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            table[i] = crc;
        }

        return table;
    }
}

/// <summary>
/// Polozka spravy: znacka zo styroch znakov (na linke v obratenom poradi bajtov), dlzka a data.
/// </summary>
/// <param name="Tag">znacka, ako ju pise log INISSu (<c>TCMD</c>, <c>RSTA</c>…)</param>
/// <param name="Data">data</param>
public sealed record RemoteChunk(string Tag, byte[] Data)
{
    /// <summary>Cele cislo (prve 4 bajty, little-endian); null pri kratsich datach.</summary>
    public int? Int32 => Data.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(Data) : null;

    /// <summary>Polozka s celym cislom.</summary>
    public static RemoteChunk FromInt32(string tag, int value)
    {
        var data = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(data, value);
        return new RemoteChunk(tag, data);
    }
}

/// <summary>
/// Sprava medzi riadiacim a vzdialenym INISSom: <c>typ dlzka poradie polozky… CRC</c> - dlzka (cela sprava vratane
/// hlavicky a suctu), poradove cislo a sucet little-endian. Riadiaci posiela typ <c>S</c>, vzdialeny odpoveda
/// <c>0xAC</c> s tym istym poradovym cislom.
/// </summary>
/// <param name="Type">typ spravy</param>
/// <param name="Sequence">poradove cislo</param>
/// <param name="Chunks">polozky</param>
/// <param name="Raw">cela sprava</param>
/// <param name="CrcOk">kontrolny sucet sedi</param>
public sealed record RemoteMessage(byte Type, uint Sequence, IReadOnlyList<RemoteChunk> Chunks, byte[] Raw, bool CrcOk)
{
    /// <summary>Sprava od riadiaceho INISSu.</summary>
    public const byte Request = 0x53;

    /// <summary>Odpoved vzdialeneho INISSu (<c>~'S'</c>).</summary>
    public const byte Reply = 0xAC;

    /// <summary>Typ, dlzka a poradove cislo.</summary>
    public const int HeaderLength = 9;

    /// <summary>Najkratsia sprava (bez poloziek).</summary>
    public const int MinLength = HeaderLength + 2;

    /// <summary>Najdlhsia sprava, ktoru INISS prijme.</summary>
    public const int MaxLength = 100_000;

    /// <summary>Prva polozka s danou znackou alebo null.</summary>
    public RemoteChunk? Find(string tag) => Chunks.FirstOrDefault(c => c.Tag == tag);

    /// <summary>Zostavi spravu s dlzkou a suctom.</summary>
    public static byte[] Build(byte type, uint sequence, IEnumerable<RemoteChunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        var body = new List<byte>();
        Span<byte> header = stackalloc byte[8];
        foreach (var chunk in chunks)
        {
            WriteTag(header, chunk.Tag);
            BinaryPrimitives.WriteInt32LittleEndian(header[4..], chunk.Data.Length);
            body.AddRange(header);
            body.AddRange(chunk.Data);
        }

        var raw = new byte[HeaderLength + body.Count + 2];
        raw[0] = type;
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(1), raw.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(5), sequence);
        body.CopyTo(raw, HeaderLength);
        BinaryPrimitives.WriteUInt16LittleEndian(raw.AsSpan(raw.Length - 2), RemoteCrc.Compute(raw.AsSpan(0, raw.Length - 2)));
        return raw;
    }

    /// <summary>Rozlozi celu spravu (dlzku uz overil <see cref="RemoteFramer" />).</summary>
    public static RemoteMessage Parse(byte[] raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var crcOk = raw.Length >= MinLength &&
                    RemoteCrc.Compute(raw.AsSpan(0, raw.Length - 2)) == BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(raw.Length - 2));
        var sequence = raw.Length >= HeaderLength ? BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(5)) : 0;
        var chunks = new List<RemoteChunk>();
        if (raw.Length >= MinLength)
        {
            // ako INISS: koniec pri menej nez 8 bajtoch alebo pri dlzke za koncom spravy
            var body = raw.AsSpan(HeaderLength, raw.Length - MinLength);
            while (body.Length >= 8)
            {
                var length = BinaryPrimitives.ReadInt32LittleEndian(body[4..]);
                if (length < 0 || length > body.Length - 8)
                    break;
                chunks.Add(new RemoteChunk(ReadTag(body), body.Slice(8, length).ToArray()));
                body = body[(8 + length)..];
            }
        }

        return new RemoteMessage(raw.Length > 0 ? raw[0] : (byte)0, sequence, chunks, raw, crcOk);
    }

    // znacka je viacznakova konstanta ulozena ako cislo little-endian - na linke ide odzadu
    private static void WriteTag(Span<byte> target, string tag)
    {
        for (var i = 0; i < 4; i++)
            target[i] = (byte)(i < tag.Length ? tag[3 - i] : ' ');
    }

    private static string ReadTag(ReadOnlySpan<byte> source)
    {
        Span<char> tag = stackalloc char[4];
        for (var i = 0; i < 4; i++)
            tag[i] = source[3 - i] is >= 0x20 and < 0x7F ? (char)source[3 - i] : '?';
        return new string(tag);
    }
}

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

/// <summary>
/// Kody vyrobcov tabul (<c>MANUFACTURER_KEY</c> katalogu ako cislo), ktore riadiaci INISS posiela v <c>TCLS</c>.
/// </summary>
public static class RemoteManufacturers
{
    private static readonly string?[] Names =
        ["Adon/BUSE", "LCD", "ERS", "FERS", "ELEN", "LCD1", "ELENOLD", "ELEN10", "ELEN16", "ELEN16Kam", "APEL", "APEL_AN", "ERP", "ELEKON"];

    /// <summary>Nazov vyrobcu podla kodu alebo null.</summary>
    public static string? Name(int code) => code >= 0 && code < Names.Length ? Names[code] : null;

    /// <summary>Text polozky (fronta, nahravky, titulok) - INISS ho posiela v kodovani Windows-1250 bez ukoncenia.</summary>
    public static string Text(byte[] data) => Encodings.Win1250.GetString(data).TrimEnd('\0');

    /// <summary>Text do polozky v kodovani Windows-1250.</summary>
    public static byte[] Bytes(string text) => Encodings.Win1250.GetBytes(text);
}
