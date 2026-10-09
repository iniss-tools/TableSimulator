namespace TableSimulator.Contracts.Boards;

/// <summary>
/// Moduly listovej tabule (ERS, FERS) a zoznamy listov - INISS posiela kody listov, text listu vie len tabula.
/// Zostavuje sa z katalogu tabule a prekodovacich tabuliek stanice (TabTab).
/// </summary>
public sealed record FlapProfileDto
{
    /// <summary>Riadkov tabule na jeden zaznam - riadok modulu = riadok tabule modulo tento pocet.</summary>
    public int LinesPerRecord { get; init; } = 1;

    /// <summary>Moduly; pozicie, ktore ziadny modul nezabera, su samostatne moduly bez zoznamu.</summary>
    public FlapModuleDto[] Modules { get; init; } = [];

    /// <summary>Zoznamy listov podla mena: kod listu → text (text zacinajuci <c>*</c> je na cervenom liste).</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Lists { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();

    /// <summary>
    /// Farby listov nastavene v simulatore: zoznam → kod → <c>black</c>, <c>red</c>, <c>yellow</c> alebo <c>white</c>; list
    /// bez farby je cierny, text zacinajuci <c>*</c> cerveny.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Colors { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();
}
