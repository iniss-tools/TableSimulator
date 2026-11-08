namespace TableSimulator.Core.Protocols.Elen;

/// <summary>
/// Kontrolny sucet ELEN - dolny bajt CRC-16/ARC zo vsetkych bajtov od <c>01</c> po <c>04</c> vratane.
/// </summary>
public static class ElenChecksum
{
    /// <summary>Kontrolny sucet bajtov <paramref name="frame" /> (od SOH po EOT vratane).</summary>
    public static byte Compute(ReadOnlySpan<byte> frame)
    {
        ushort crc = 0;
        foreach (var b in frame)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
        }

        return (byte)crc;
    }

    /// <summary>Sucet rozdeleny na dva bajty s hornym bitom (tak, ako sa posiela).</summary>
    public static (byte High, byte Low) Encode(byte sum) => ((byte)(0x80 | (sum >> 4)), (byte)(0x80 | (sum & 0x0F)));
}
