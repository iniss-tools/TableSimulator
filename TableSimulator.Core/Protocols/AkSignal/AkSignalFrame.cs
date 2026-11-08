namespace TableSimulator.Core.Protocols.AkSignal;

/// <summary>
/// Prikaz servera INISS2 ustredni AK Signal: <c>adresa dlzka prikaz [hodnota hodnota] sucet</c>, dlzka 4 alebo 6 (prikaz
/// <see cref="SetAudio" /> s hodnotou dolnym bajtom napred).
/// </summary>
/// <param name="Address">adresa ustredne 1-255</param>
/// <param name="Command">prikaz</param>
/// <param name="Value">hodnota prikazu s dlzkou 6; null = prikaz bez hodnoty</param>
/// <param name="Raw">cela sprava</param>
/// <param name="ChecksumOk">sucet sedi</param>
public sealed record AkSignalFrame(int Address, byte Command, int? Value, byte[] Raw, bool ChecksumOk)
{
    /// <summary>Dotaz na stav rozhlasu.</summary>
    public const byte GetAudio = 0x10;

    /// <summary>Vypnut rozhlas.</summary>
    public const byte ClearAudio = 0x11;

    /// <summary>Zapnut rozhlas s dvojbajtovou hodnotou.</summary>
    public const byte SetAudio = 0x12;

    /// <summary>Dotaz na stav svetla.</summary>
    public const byte GetLight = 0x20;

    /// <summary>Vypnut automaticke svietenie.</summary>
    public const byte ClearLight = 0x21;

    /// <summary>Zapnut automaticke svietenie.</summary>
    public const byte SetLight = 0x22;

    /// <summary>Zapnut svetlo rucne.</summary>
    public const byte SetManual = 0x23;

    /// <summary>Zrusit rucne svietenie.</summary>
    public const byte ClearManual = 0x24;

    /// <summary>Hodnota zapnutia rozhlasu bez <c>daymode</c>/<c>nightmode</c> v Audio.xml.</summary>
    public const int NoValue = 0xFFFF;

    /// <summary>Dlzka odpovede ustredne.</summary>
    public const int AnswerLength = 5;

    /// <summary>Sucet: dolnych 8 bitov suctu bajtov.</summary>
    public static byte Checksum(ReadOnlySpan<byte> bytes)
    {
        var sum = 0;
        foreach (var b in bytes)
            sum += b;
        return (byte)sum;
    }

    /// <summary>Prikaz servera - pri <see cref="SetAudio" /> s hodnotou <paramref name="value" />.</summary>
    public static byte[] Build(int address, byte command, int value = NoValue)
    {
        byte[] frame = command == SetAudio
            ? [(byte)address, 6, command, (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF), 0]
            : [(byte)address, 4, command, 0];
        frame[^1] = Checksum(frame.AsSpan(0, frame.Length - 1));
        return frame;
    }

    /// <summary>Odpoved ustredne: <c>00 05 prikaz stav sucet</c> - adresu nenesie.</summary>
    public static byte[] Answer(byte command, byte state)
    {
        byte[] frame = [0x00, AnswerLength, command, state, 0];
        frame[^1] = Checksum(frame.AsSpan(0, AnswerLength - 1));
        return frame;
    }

    /// <summary>Prikaz pre svetlo (inak pre rozhlas).</summary>
    public static bool IsLight(byte command) => command is >= GetLight and <= ClearManual;
}
