using System.Buffers.Binary;

namespace TableSimulator.Core.Protocols.Remote;

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
