using Microsoft.AspNetCore.Identity;

namespace TableSimulator.Persistence.Identity;

/// <summary>
/// Rola: vstavane (<c>admin</c>, <c>operator</c>, <c>viewer</c>) a vlastne roly spravcu. <c>Name</c> je kluc roly
/// (v prihlaseni a pri pristupovych klucoch), nemeni sa.
/// </summary>
public sealed class AppRole : IdentityRole<Guid>
{
    public AppRole()
    {
    }

    public AppRole(string name)
        : base(name)
    {
    }

    /// <summary>Nazov vlastnej roly; pri vstavanej prazdny (nazov dava rozhranie podla jazyka).</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Opravnenia; null = predvolene vstavanej roly (doplni ich start servera). Spravca ma vzdy vsetky.</summary>
    public List<string>? Permissions { get; set; }
}
