namespace TableSimulator.Core.Protocols.Elsvo;

/// <summary>
/// Sprava ELSVO - prikaz INISSu aj odpoved jednotky maju pat bajtov <c>02 adresa prikaz/stav 03 sucet</c>.
/// </summary>
/// <param name="Address">adresa jednotky</param>
/// <param name="Code">prikaz (od INISSu) alebo stav (od jednotky)</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">sucet sedi</param>
public sealed record ElsvoFrame(int Address, byte Code, byte[] Raw, bool ChecksumOk)
{
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;
    public const int Length = 5;

    /// <summary>Zapnut zosilnovac.</summary>
    public const byte On = 0x1F;

    /// <summary>Zapnut zosilnovac v druhom dennom pasme okruhu.</summary>
    public const byte OnBand2 = 0x3F;

    /// <summary>Dotaz na stav.</summary>
    public const byte Query = 0x80;

    /// <summary>Uvolnit po skonceni hlasenia.</summary>
    public const byte Release = 0xC0;

    /// <summary>Vypnut.</summary>
    public const byte Off = 0x00;

    /// <summary>Sucet doplna sucet vsetkych piatich bajtov do nuly (zaporny sucet prvych styroch).</summary>
    public static byte Checksum(int address, byte code) => (byte)-(Stx + address + code + Etx);

    /// <summary>Sprava s adresou a prikazom alebo stavom.</summary>
    public static byte[] Build(int address, byte code) => [Stx, (byte)address, code, Etx, Checksum(address, code)];
}
