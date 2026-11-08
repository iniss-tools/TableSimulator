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
