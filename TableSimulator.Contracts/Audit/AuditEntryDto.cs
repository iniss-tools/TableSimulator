namespace TableSimulator.Contracts.Audit;

/// <summary>Zaznam auditu - kto co v simulatore urobil.</summary>
/// <param name="Id">poradie zaznamu (rastie)</param>
/// <param name="Time">cas (UTC)</param>
/// <param name="User">prihlasovacie meno; <c>-</c> = neprihlaseny (napr. neuspesne prihlasenie)</param>
/// <param name="Action">akcia - meno prikazu (<c>SetLine</c>, <c>Login</c>…)</param>
/// <param name="Target">cim sa akcia tykala (cislo linky, tabula, pouzivatel…); prazdne = celym simulatorom</param>
/// <param name="Success">akcia sa podarila</param>
/// <param name="Error">kod chyby, ak sa nepodarila</param>
/// <param name="Details">podrobnosti (JSON prikazu bez hesiel a velkych obsahov)</param>
/// <param name="Address">IP adresa, z ktorej poziadavka prisla</param>
public sealed record AuditEntryDto(
    long Id,
    DateTime Time,
    string User,
    string Action,
    string Target,
    bool Success,
    string? Error,
    string Details,
    string? Address);
