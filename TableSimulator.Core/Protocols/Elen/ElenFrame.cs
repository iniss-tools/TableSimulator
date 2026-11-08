namespace TableSimulator.Core.Protocols.Elen;

/// <summary>
/// Sprava od INISSu: <c>01 adresa|80 prikaz|80 parametre… 04 sucetH|80 sucetL|80</c>.
/// </summary>
/// <param name="Address">adresa tabule 0-127; 0 = hromadna sprava</param>
/// <param name="Command">cislo prikazu</param>
/// <param name="Parameters">parametre medzi prikazom a EOT</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">kontrolny sucet sedi</param>
public sealed record ElenFrame(int Address, int Command, byte[] Parameters, byte[] Raw, bool ChecksumOk)
{
    public const byte Soh = 0x01;
    public const byte Eot = 0x04;
    public const byte Ack = 0x06;
    public const byte Nak = 0x15;

    /// <summary>Hromadna sprava pre vsetky tabule na linke.</summary>
    public bool IsBroadcast => Address == 0;

    /// <summary>
    /// V obsahu je bajt <c>04</c> (EOT) - napr. cislo pisma bez bitu <c>0x40</c>. Skladac spravu dokoncil az pri
    /// skutocnom konci (za EOT nesli dva bajty suctu s hornym bitom), tabula ELEN by ju vsak ukoncila uz pri nom.
    /// </summary>
    public bool EotInData => Parameters.AsSpan().Contains(Eot);

    /// <summary>
    /// Zostavi spravu (na testy a nastroje): doplni rámec a kontrolny sucet.
    /// </summary>
    public static byte[] Build(int address, int command, ReadOnlySpan<byte> parameters)
    {
        var raw = new byte[parameters.Length + 6];
        raw[0] = Soh;
        raw[1] = (byte)(0x80 | address);
        raw[2] = (byte)(0x80 | command);
        parameters.CopyTo(raw.AsSpan(3));
        raw[^3] = Eot;
        (raw[^2], raw[^1]) = ElenChecksum.Encode(ElenChecksum.Compute(raw.AsSpan(0, raw.Length - 2)));
        return raw;
    }
}
