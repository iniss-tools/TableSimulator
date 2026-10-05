namespace TableSimulator.Application.Audit;

/// <summary>Zaznam auditu ulozeny v databaze.</summary>
/// <param name="Id">poradie (pridelí databaza)</param>
/// <param name="Time">cas (UTC)</param>
/// <param name="User">prihlasovacie meno; <c>-</c> = neprihlaseny</param>
/// <param name="Action">meno prikazu bez pripony <c>Command</c></param>
/// <param name="Target">cim sa akcia tykala</param>
/// <param name="Success">akcia sa podarila</param>
/// <param name="Error">kod chyby</param>
/// <param name="Details">podrobnosti (<see cref="AuditDetails" />)</param>
/// <param name="Address">IP adresa poziadavky</param>
public sealed record AuditEntry(
    long Id,
    DateTime Time,
    string User,
    string Action,
    string Target,
    bool Success,
    string? Error,
    string Details,
    string? Address)
{
    /// <summary>Kolko dni sa zaznamy drzia.</summary>
    public const int RetentionDays = 365;
}
