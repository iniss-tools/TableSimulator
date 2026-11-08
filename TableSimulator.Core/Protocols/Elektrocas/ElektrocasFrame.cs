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
