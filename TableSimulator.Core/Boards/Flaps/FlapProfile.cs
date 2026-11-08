namespace TableSimulator.Core.Boards.Flaps;

/// <summary>
/// Moduly listovej tabule a zoznamy listov - INISS posiela kody listov, co na liste je, vie len tabula. Zostavuje sa
/// z katalogu tabule a prekodovacich tabuliek stanice (<c>ToolsCore.Iniss.Tables.FlapLayouts</c>).
/// </summary>
public sealed record FlapProfile
{
    /// <summary>Riadkov tabule na jeden zaznam - riadok modulu = riadok tabule modulo tento pocet.</summary>
    public int LinesPerRecord { get; init; } = 1;

    /// <summary>Moduly; pozicie, ktore ziadny modul nezabera, su samostatne moduly bez zoznamu.</summary>
    public IReadOnlyList<FlapModule> Modules { get; init; } = [];

    /// <summary>Zoznamy listov podla mena: kod listu → text (text zacinajuci <c>*</c> je na cervenom liste).</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Lists { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();

    /// <summary>
    /// Farby listov nastavene v simulatore (zoznam → kod → <see cref="FlapColors" />) - fyzicka vlastnost listu, ktoru
    /// data stanice nemaju; list bez farby je cierny, text zacinajuci <c>*</c> cerveny.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Colors { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();
}
