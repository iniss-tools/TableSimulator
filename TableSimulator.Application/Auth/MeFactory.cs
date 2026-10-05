using TableSimulator.Contracts.Auth;

namespace TableSimulator.Application.Auth;

/// <summary>Prihlaseny pouzivatel pre webove rozhranie.</summary>
internal static class MeFactory
{
    public static MeDto Create(bool loginRequired, bool setupRequired, string? userName, string? displayName, string? role) =>
        new(loginRequired, setupRequired, userName is not null, userName, displayName, role, Roles.PermissionsOf(role));

    public static MeDto Create(IAuthService auth, SignedInUser user) =>
        Create(auth.LoginRequired, false, user.UserName, user.DisplayName, user.Role);
}
