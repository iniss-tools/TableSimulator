namespace TableSimulator.Contracts.OAuth;

/// <summary>Rola na vyber na stranke povolenia - pouzivatel bez spravy roli zoznam roli nevidi.</summary>
/// <param name="Name">kluc roly</param>
/// <param name="DisplayName">nazov vlastnej roly; prazdny pri vstavanej (web ju prelozi)</param>
public sealed record OAuthRoleDto(string Name, string DisplayName);
