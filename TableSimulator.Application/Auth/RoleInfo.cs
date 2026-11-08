namespace TableSimulator.Application.Auth;

/// <summary>Rola a jej opravnenia.</summary>
/// <param name="Name">kluc roly (v prihlaseni a pri pristupovych klucoch); nemeni sa</param>
/// <param name="DisplayName">nazov vlastnej roly; pri vstavanej prazdny - nazov dava rozhranie podla jazyka</param>
/// <param name="Permissions">opravnenia</param>
public sealed record RoleInfo(string Name, string DisplayName, IReadOnlyList<string> Permissions);
