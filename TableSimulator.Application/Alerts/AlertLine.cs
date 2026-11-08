namespace TableSimulator.Application.Alerts;

/// <summary>Linka v oznameni.</summary>
/// <param name="Number">cislo linky</param>
/// <param name="Name">nazov linky</param>
/// <param name="Status">stav (kluc ako v API: <c>lost</c>, <c>error</c>, <c>connected</c>; alebo <see cref="AlertLineState.Silent" />)</param>
/// <param name="Detail">podrobnost (chyba linky, ustredne, ktorych sa server nepyta)</param>
/// <param name="Since">od kedy stav trva</param>
public sealed record AlertLine(int Number, string Name, string Status, string? Detail, DateTimeOffset Since)
{
    /// <summary>Pri navrate stav pocas vypadku; inak null.</summary>
    public string? Previous { get; init; }
}
