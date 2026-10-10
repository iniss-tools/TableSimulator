namespace TableSimulator.Contracts.Auth;

/// <summary>Prihlaseny pouzivatel a rezim prihlasenia (GET /api/auth/me).</summary>
/// <param name="LoginRequired">server vyzaduje prihlasenie (inak je kazdy na tomto pocitaci spravca)</param>
/// <param name="SetupRequired">este nie je ziaden pouzivatel - treba zalozit prveho spravcu</param>
/// <param name="Authenticated">poziadavka je prihlasena</param>
/// <param name="UserName">prihlasovacie meno; null = neprihlaseny</param>
/// <param name="DisplayName">meno pre zobrazenie</param>
/// <param name="Role">kluc roly (vstavane <c>admin</c>, <c>operator</c>, <c>viewer</c> alebo vlastna); null = neprihlaseny</param>
/// <param name="RoleName">nazov vlastnej roly; null = vstavana (nazov podla jazyka rozhrania) alebo neprihlaseny</param>
/// <param name="Permissions">opravnenia roly - rozhranie podla nich skryje, co pouzivatel nesmie</param>
public sealed record MeDto(
    bool LoginRequired,
    bool SetupRequired,
    bool Authenticated,
    string? UserName,
    string? DisplayName,
    string? Role,
    string? RoleName,
    IReadOnlyList<string> Permissions)
{
    /// <summary>Poziadavka prisla s pristupovym klucom (zariadenie, program) - nema heslo ani odhlasenie.</summary>
    public bool AccessKey { get; init; }
}
