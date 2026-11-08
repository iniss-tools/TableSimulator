namespace TableSimulator.Application.Devices.Sounds;

/// <summary>
/// Banka zvukov programu INISSu: dlzka a subor nahravky podla mena, ako ho INISS posiela zariadeniu (server INISS2
/// <see cref="SoundLengths" />, INISS 3.39 <see cref="Iniss339SoundLengths" />).
/// </summary>
public interface ISoundBank
{
    /// <summary>Dlzka nahravky; null = banka ju nepozna alebo sa neda precitat.</summary>
    TimeSpan? Length(string sound);

    /// <summary>Subor nahravky (.EWA alebo WAV); null = banka ju nepozna alebo subor nie je v priecinku banky.</summary>
    string? FileOf(string sound);
}
