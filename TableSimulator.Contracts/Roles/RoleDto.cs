namespace TableSimulator.Contracts.Roles;

/// <summary>Rola s opravneniami a poctom pouzivatelov a klucov, ktore ju maju.</summary>
/// <param name="Name">kluc roly (pri pouzivateloch a klucoch); nemeni sa</param>
/// <param name="DisplayName">nazov vlastnej roly; pri vstavanej prazdny - nazov podla jazyka rozhrania</param>
/// <param name="BuiltIn">vstavana rola (<c>admin</c>, <c>operator</c>, <c>viewer</c>) - neda sa zmazat, spravca ani upravit</param>
/// <param name="Permissions">opravnenia (kluce ako politiky servera)</param>
/// <param name="Users">pocet pouzivatelov s rolou</param>
/// <param name="Keys">pocet pristupovych klucov s rolou</param>
public sealed record RoleDto(string Name, string DisplayName, bool BuiltIn, IReadOnlyList<string> Permissions, int Users, int Keys);
