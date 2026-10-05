using Microsoft.AspNetCore.Identity;

namespace TableSimulator.Persistence.Identity;

/// <summary>Pouzivatel simulatora.</summary>
public sealed class AppUser : IdentityUser<Guid>
{
    /// <summary>Meno pre zobrazenie.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Kedy ucet vznikol.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Ucet je vypnuty - prihlasit sa neda, existujuce prihlasenie sa zrusi.</summary>
    public bool IsDisabled { get; set; }
}
