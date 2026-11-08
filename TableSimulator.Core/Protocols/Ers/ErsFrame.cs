using System.Globalization;
using System.Text;

namespace TableSimulator.Core.Protocols.Ers;

/// <summary>
/// Sprava ERS od INISSu: zapis riadku <c>16 16 01 adresa 02 'D' 'L' nnn text 03 BCC</c> alebo dotaz
/// <c>16 16 01 adresa 05</c>.
/// </summary>
/// <param name="Address">adresa tabule</param>
/// <param name="Command">prikaz (<c>DL</c>); null pri dotaze</param>
/// <param name="Data">znaky za prikazom (cislo riadku a kody listov); prazdne pri dotaze</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">BCC sedi (dotaz nema)</param>
public sealed record ErsFrame(int Address, string? Command, byte[] Data, byte[] Raw, bool ChecksumOk)
{
    public const byte Syn = 0x16;
    public const byte Soh = 0x01;
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;
    public const byte Enq = 0x05;
    public const byte Ack = 0x06;
    public const byte Nak = 0x15;
    public const byte Can = 0x18;

    /// <summary>Prikaz zapisu riadku.</summary>
    public const string WriteRow = "DL";

    /// <summary>Dotaz, ci tabula dokoncila.</summary>
    public bool IsQuery => Command is null;

    /// <summary>Kontrolny sucet: sucet bajtov od STX po ETX vratane, dolnych 7 bitov.</summary>
    public static byte Bcc(ReadOnlySpan<byte> fromStxToEtx)
    {
        var sum = 0;
        foreach (var b in fromStxToEtx)
            sum += b;
        return (byte)(sum & 0x7F);
    }

    /// <summary>Zapis riadku (na testy a nastroje).</summary>
    public static byte[] Build(int address, int row, string codes)
    {
        var body = Encoding.ASCII.GetBytes("DL" + row.ToString("000", CultureInfo.InvariantCulture) + codes);
        byte[] raw = [Syn, Syn, Soh, (byte)address, Stx, .. body, Etx, 0];
        raw[^1] = Bcc(raw.AsSpan(4, raw.Length - 5));
        return raw;
    }

    /// <summary>Dotaz (na testy a nastroje).</summary>
    public static byte[] Query(int address) => [Syn, Syn, Soh, (byte)address, Enq];

    /// <summary>Kratka odpoved tabule: <c>16 16 01 adresa kod</c> (ACK, NAK, CAN).</summary>
    public static byte[] Short(int address, byte code) => [Syn, Syn, Soh, (byte)address, code];

    /// <summary>Stavova sprava: <c>16 16 01 adresa 02 d0…d7 03 BCC</c>, kod stavu v d3 a d4 (dve cislice ASCII).</summary>
    public static byte[] Status(int address, int code)
    {
        var digits = Encoding.ASCII.GetBytes("000" + Math.Clamp(code, 0, 99).ToString("00", CultureInfo.InvariantCulture) + "000");
        byte[] raw = [Syn, Syn, Soh, (byte)address, Stx, .. digits, Etx, 0];
        raw[^1] = Bcc(raw.AsSpan(4, raw.Length - 5));
        return raw;
    }
}
