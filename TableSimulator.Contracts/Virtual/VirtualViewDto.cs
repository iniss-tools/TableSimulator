namespace TableSimulator.Contracts.Virtual;

/// <summary>Co ukazuje virtualna tabula (GET /api/virtual/{cislo}).</summary>
/// <param name="Board">definicia tabule</param>
/// <param name="Source">subor XML; null, ak v priecinkoch nie je</param>
/// <param name="Rows">vybrane zaznamy s nahradenym oddelovacom</param>
/// <param name="Layout">rozlozenie tabule (predvolene, ak jej rozlozenie uz neexistuje)</param>
public sealed record VirtualViewDto(VirtualBoardDto Board, VirtualSourceDto? Source, IReadOnlyList<VirtualRowDto> Rows, VirtualLayoutDto Layout);
