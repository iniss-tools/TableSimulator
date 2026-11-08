namespace TableSimulator.Application.Alerts;

/// <summary>Stav linky pre oznamenia: vypadok, pripojena alebo nic z toho (caka, zastavena).</summary>
/// <param name="Number">cislo linky</param>
/// <param name="Name">nazov</param>
/// <param name="Status">
/// stav ako v API (<c>lost</c>, <c>error</c>, <c>connected</c>, <c>listening</c>, <c>stopped</c>) alebo <see cref="Silent" />
/// </param>
/// <param name="Detail">podrobnost stavu (pri <see cref="Silent" /> ustredne, ktorych sa server nepyta)</param>
/// <param name="Since">od kedy stav trva</param>
public sealed record AlertLineState(int Number, string Name, string Status, string? Detail, DateTimeOffset Since)
{
    /// <summary>Linka funguje, ale server INISS2 sa jej ustredne AK Signal dlhsie nepyta.</summary>
    public const string Silent = "silent";

    /// <summary>INISS sa odpojil, linka nebezi alebo sa server ustredne nepyta.</summary>
    public bool IsOutage => Status is "lost" or "error" or Silent;
}
