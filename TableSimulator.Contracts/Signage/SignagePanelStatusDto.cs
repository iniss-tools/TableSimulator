namespace TableSimulator.Contracts.Signage;

/// <summary>Posledne spojenie panelu (od startu servera).</summary>
/// <param name="LastSeenAt">kedy si naposledy vypytal program</param>
/// <param name="Address">adresa zariadenia</param>
/// <param name="Online">pytal sa v poslednych minutach</param>
public sealed record SignagePanelStatusDto(DateTimeOffset LastSeenAt, string Address, bool Online);
