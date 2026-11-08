using System.Buffers.Binary;

namespace TableSimulator.Core.Protocols.Remote;

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
