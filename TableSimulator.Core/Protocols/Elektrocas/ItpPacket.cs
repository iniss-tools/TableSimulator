namespace TableSimulator.Core.Protocols.Elektrocas;

/// <summary>
/// Paket ITP pre prevodnik KTP (TableClass 130): 16-bitove slova najprv dolnym bajtom -
/// <c>8FF1 dlzka druh cislo vnutorna_sprava… sucet 8EE1</c>; dlzka v slovach, sucet = slova 1 az posledne slovo
/// vnutornej spravy. Viz docs/iniss/protokoly/itp.mdx.
/// </summary>
/// <param name="Kind">druh (slovo 2): <c>0x80</c> prikaz, dolnych 6 bitov vnutorny protokol, horny bajt dlzka hlavicky</param>
/// <param name="Sequence">poradove cislo spravy (odpoved ho vracia)</param>
/// <param name="Payload">vnutorna sprava vratane pripadneho doplnkoveho bajtu 00</param>
/// <param name="Raw">cely paket</param>
/// <param name="ChecksumOk">sucet sedi a paket konci <c>8EE1</c></param>
public sealed record ItpPacket(int Kind, int Sequence, byte[] Payload, byte[] Raw, bool ChecksumOk)
{
    /// <summary>Zaciatok paketu.</summary>
    public const ushort Start = 0x8FF1;

    /// <summary>Prvy bajt paketu (dolny bajt zaciatku).</summary>
    internal const byte StartLow = Start & 0xFF;

    /// <summary>Koniec paketu.</summary>
    public const ushort End = 0x8EE1;

    /// <summary>Druh: prikaz (v odpovedi nulovy).</summary>
    public const int CommandFlag = 0x80;

    /// <summary>Vnutorny protokol KTP (spravy tabul Elektrocas).</summary>
    public const int KtpProtocol = 0x20;

    /// <summary>Najkratsi paket v slovach (zaciatok, dlzka, druh, cislo, sucet, koniec).</summary>
    public const int MinWords = 6;

    /// <summary>Najdlhsi paket, ktory INISS prijme, v bajtoch.</summary>
    public const int MaxBytes = 100_000;

    /// <summary>Vnutorny protokol (dolnych 6 bitov druhu).</summary>
    public int Protocol => Kind & 0x3F;

    /// <summary>Paket je prikaz (nie odpoved).</summary>
    public bool IsCommand => (Kind & CommandFlag) != 0;

    /// <summary>Zostavi paket; vnutornu spravu neparnej dlzky doplni bajtom 00.</summary>
    public static byte[] Build(int kind, int sequence, ReadOnlySpan<byte> payload)
    {
        var padded = (payload.Length + 1) / 2 * 2;
        var raw = new byte[8 + padded + 4];
        var words = raw.Length / 2;
        Put(raw, 0, Start);
        Put(raw, 1, (ushort)words);
        Put(raw, 2, (ushort)kind);
        Put(raw, 3, (ushort)sequence);
        payload.CopyTo(raw.AsSpan(8));
        Put(raw, words - 2, Sum(raw, words));
        Put(raw, words - 1, End);
        return raw;
    }

    /// <summary>Sucet slov 1 az posledne slovo vnutornej spravy (dolnych 16 bitov).</summary>
    internal static ushort Sum(byte[] raw, int words)
    {
        var sum = 0;
        for (var i = 1; i <= words - 3; i++)
            sum += Word(raw, i);
        return (ushort)sum;
    }

    internal static ushort Word(byte[] raw, int index) => (ushort)(raw[2 * index] | raw[2 * index + 1] << 8);

    private static void Put(byte[] raw, int index, ushort value)
    {
        raw[2 * index] = (byte)value;
        raw[2 * index + 1] = (byte)(value >> 8);
    }
}
