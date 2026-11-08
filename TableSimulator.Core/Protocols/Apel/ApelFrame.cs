namespace TableSimulator.Core.Protocols.Apel;

/// <summary>
/// Sprava APEL od INISSu: <c>10 02 adresa [prikaz dlzka data…]… sucet 10 03</c> - bajt <c>10</c> vnutri spravy je
/// zdvojeny, sucet je XOR vsetkych bajtov od prvého prikazu po koniec dat (bez adresy). Jedna sprava moze niest viac
/// zaznamov (zmazanie = prazdny text do viacerych zasobnikov).
/// </summary>
/// <param name="Address">adresa tabule; <c>FF</c> = vsetky</param>
/// <param name="Records">zaznamy</param>
/// <param name="Raw">cela sprava, ako prisla (so zdvojenymi bajtmi)</param>
/// <param name="ChecksumOk">sucet sedi a zaznamy presne vyplnia spravu</param>
public sealed record ApelFrame(int Address, IReadOnlyList<ApelRecord> Records, byte[] Raw, bool ChecksumOk)
{
    public const byte Dle = 0x10;
    public const byte Stx = 0x02;
    public const byte Etx = 0x03;

    /// <summary>Adresa vsetkych tabul.</summary>
    public const int Broadcast = 0xFF;

    /// <summary>Predvolena adresa pocitaca (<c>ComputerAddr</c> linky triedy 10) - tabula ju vracia v odpovedi.</summary>
    public const int DefaultComputerAddress = 0x80;

    /// <summary>Stavy v odpovedi tabule.</summary>
    public const byte StateOk = 0x06;

    public const byte StateBusy = 0x07;
    public const byte StateLostConfig = 0x08;
    public const byte StateTransmissionError = 0x15;

    /// <summary>Obsah spravy bez ramca: adresa, zaznamy a sucet (na zabalenie).</summary>
    public static byte[] Payload(int address, IEnumerable<ApelRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var body = new List<byte>();
        foreach (var record in records)
        {
            body.Add((byte)record.Command);
            body.Add((byte)record.Data.Length);
            body.AddRange(record.Data);
        }

        return [(byte)address, .. body, Xor(body.ToArray())];
    }

    /// <summary>Cela sprava (na testy a nastroje).</summary>
    public static byte[] Build(int address, params ApelRecord[] records) => Wrap(Payload(address, records));

    /// <summary>Odpoved tabule: <c>10 02 ComputerAddr adresa stav sucet 10 03</c>, sucet = XOR adresy a stavu.</summary>
    public static byte[] Reply(int computerAddress, int address, byte state) =>
        Wrap([(byte)computerAddress, (byte)address, state, (byte)(address ^ state)]);

    /// <summary>Zabali obsah do <c>10 02 … 10 03</c> a zdvoji bajty <c>10</c>.</summary>
    public static byte[] Wrap(ReadOnlySpan<byte> payload)
    {
        var raw = new List<byte>(payload.Length + 6) { Dle, Stx };
        foreach (var b in payload)
        {
            raw.Add(b);
            if (b == Dle)
                raw.Add(Dle);
        }

        raw.Add(Dle);
        raw.Add(Etx);
        return raw.ToArray();
    }

    /// <summary>XOR bajtov.</summary>
    public static byte Xor(ReadOnlySpan<byte> bytes)
    {
        byte sum = 0;
        foreach (var b in bytes)
            sum ^= b;
        return sum;
    }

    /// <summary>Rozlozi obsah spravy (bez ramca a zdvojeni): adresa, zaznamy a sucet.</summary>
    public static ApelFrame Parse(ReadOnlySpan<byte> payload, byte[] raw)
    {
        if (payload.Length < 2)
            return new ApelFrame(payload.Length > 0 ? payload[0] : 0, [], raw, false);
        var body = payload[1..^1];
        var records = new List<ApelRecord>();
        var i = 0;
        var complete = true;
        while (i < body.Length)
        {
            if (i + 2 > body.Length || i + 2 + body[i + 1] > body.Length)
            {
                complete = false;
                break;
            }

            records.Add(new ApelRecord(body[i], body.Slice(i + 2, body[i + 1]).ToArray()));
            i += 2 + body[i + 1];
        }

        return new ApelFrame(payload[0], records, raw, complete && Xor(body) == payload[^1]);
    }
}
