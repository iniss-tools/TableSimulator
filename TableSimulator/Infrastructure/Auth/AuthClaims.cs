using System.Security.Claims;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Vlastne udaje v prihlaseni.</summary>
public static class AuthClaims
{
    /// <summary>Meno pre zobrazenie.</summary>
    public const string DisplayName = "display_name";

    /// <summary>Identifikator pristupoveho kluca - poziadavka nie je od prihlaseneho pouzivatela.</summary>
    public const string AccessKey = "access_key";

    /// <summary>Rola v prihlaseni (pouzivatel ma prave jednu); null = ziadna.</summary>
    public static string? RoleOf(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.Identities.Where(i => i.IsAuthenticated).SelectMany(i => i.FindAll(i.RoleClaimType)).Select(c => c.Value).FirstOrDefault();
    }
}
