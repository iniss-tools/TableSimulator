namespace TableSimulator.Application.Auth;

/// <summary>Rola a jej opravnenia.</summary>
/// <param name="Name">kluc roly (v prihlaseni a pri pristupovych klucoch); nemeni sa</param>
/// <param name="DisplayName">nazov vlastnej roly; pri vstavanej prazdny - nazov dava rozhranie podla jazyka</param>
/// <param name="Permissions">opravnenia</param>
public sealed record RoleInfo(string Name, string DisplayName, IReadOnlyList<string> Permissions);

/// <summary>
/// Roly a ich opravnenia v pamati - autorizacia kazdej poziadavky sa pyta tu, nie databazy. Server ich nacita pri
/// starte a po kazdej zmene roly, takze zmena opravneni plati hned. Spravca ma vzdy vsetky opravnenia; nezname
/// kluce opravneni sa vynechaju.
/// </summary>
public sealed class RoleCatalog
{
    private volatile IReadOnlyDictionary<string, RoleInfo> _roles = Index(Roles.BuiltIn.Select(r => new RoleInfo(r, "", Roles.DefaultsOf(r))));

    /// <summary>Vsetky roly: vstavane od najvyssej, potom vlastne podla nazvu.</summary>
    public IReadOnlyList<RoleInfo> All =>
    [
        .. Roles.BuiltIn.Where(_roles.ContainsKey).Select(r => _roles[r]),
        .. _roles.Values.Where(r => !Roles.IsBuiltIn(r.Name)).OrderBy(r => r.DisplayName, StringComparer.CurrentCultureIgnoreCase)
    ];

    /// <summary>Nahradi roly (pri starte a po zmene v databaze).</summary>
    public void Load(IEnumerable<RoleInfo> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        _roles = Index(roles);
    }

    /// <summary>Rola podla kluca; null = neznama.</summary>
    public RoleInfo? Find(string? role) => role is not null && _roles.TryGetValue(role, out var info) ? info : null;

    /// <summary>Opravnenia roly; neznama rola nema ziadne.</summary>
    public IReadOnlyList<string> PermissionsOf(string? role) => Find(role)?.Permissions ?? [];

    /// <summary>Rola ma opravnenie.</summary>
    public bool Has(string? role, string permission) => PermissionsOf(role).Contains(permission, StringComparer.Ordinal);

    /// <summary>Rola <paramref name="role" /> ma vsetky <paramref name="permissions" /> - pouzivatel nesmie pridelit viac, nez sam ma.</summary>
    public bool Covers(string? role, IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var own = PermissionsOf(role);
        return permissions.All(p => own.Contains(p, StringComparer.Ordinal));
    }

    private static Dictionary<string, RoleInfo> Index(IEnumerable<RoleInfo> roles) =>
        roles.Select(r => r with
            {
                Permissions = r.Name == Roles.Admin ? Permissions.All : Permissions.All.Where(p => r.Permissions.Contains(p, StringComparer.Ordinal)).ToArray()
            })
            .ToDictionary(r => r.Name, StringComparer.Ordinal);
}
