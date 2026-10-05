namespace TableSimulator.Infrastructure.Auth;

/// <summary>Vlastne udaje v prihlaseni.</summary>
public static class AuthClaims
{
    /// <summary>Meno pre zobrazenie.</summary>
    public const string DisplayName = "display_name";

    /// <summary>Identifikator pristupoveho kluca - poziadavka nie je od prihlaseneho pouzivatela.</summary>
    public const string AccessKey = "access_key";
}
