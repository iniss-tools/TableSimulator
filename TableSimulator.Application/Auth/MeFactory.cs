using TableSimulator.Contracts.Auth;

namespace TableSimulator.Application.Auth;

/// <summary>Prihlaseny pouzivatel pre webove rozhranie.</summary>
internal static class MeFactory
{
    public static MeDto Create(RoleCatalog roles, bool loginRequired, bool setupRequired, string? userName, string? displayName, string? role)
    {
        var info = roles.Find(role);
        return new MeDto(loginRequired, setupRequired, userName is not null, userName, displayName, role,
            info is null || Roles.IsBuiltIn(info.Name) ? null : info.DisplayName, roles.PermissionsOf(role));
    }

    /// <summary>Pouzivatel poziadavky (prihlaseny, s pristupovym klucom alebo neprihlaseny).</summary>
    public static MeDto Create(RoleCatalog roles, bool loginRequired, bool setupRequired, ICurrentUser user) =>
        user.IsAuthenticated
            ? Create(roles, loginRequired, setupRequired, user.UserName, user.DisplayName, user.Role) with { AccessKey = user.IsAccessKey }
            : Create(roles, loginRequired, setupRequired, null, null, null);

    public static MeDto Create(RoleCatalog roles, IAuthService auth, SignedInUser user) =>
        Create(roles, auth.LoginRequired, false, user.UserName, user.DisplayName, user.Role);
}
