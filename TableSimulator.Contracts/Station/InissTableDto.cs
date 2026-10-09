namespace TableSimulator.Contracts.Station;

/// <summary>Fyzicka tabula instalacie INISSu.</summary>
/// <param name="Name">nazov (KEY z TPhysic)</param>
/// <param name="Address">adresa (ID z TPhysic)</param>
/// <param name="Grafikon">grafikon (priecinok), z ktoreho tabula pochadza</param>
/// <param name="Manufacturer">vyrobca z katalogovej predlohy; prazdny = neznamy</param>
/// <param name="Automatic">linku jej INISS priradi automaticky (COMUNICATION_PORT=0)</param>
/// <param name="Shown">simulator ju na linke zobrazi (pozna vyrobcu pre triedu linky)</param>
/// <param name="Problem">preco jej INISS nic neposle; null = posiela</param>
public sealed record InissTableDto(string Name, int Address, string Grafikon, string Manufacturer, bool Automatic, bool Shown, InissProblemDto? Problem)
{
    /// <summary>Listova tabula: moduly a zoznamy listov z katalogu a TabTab; null = nie je listova.</summary>
    public Boards.FlapProfileDto? Flaps { get; init; }

    /// <summary>Listova tabula: pocet zaznamov (MAX_REC_COUNT).</summary>
    public int FlapRecords { get; init; } = 1;
}
