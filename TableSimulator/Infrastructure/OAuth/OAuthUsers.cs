using Microsoft.AspNetCore.Identity;
using TableSimulator.Application.Auth;
using TableSimulator.Application.OAuth;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Infrastructure.OAuth;

/// <summary>Pouzivatel povolenia OAuth: ci stale existuje, nie je vypnuty a jeho rola pokryva rolu klienta.</summary>
public sealed class OAuthUsers(UserManager<AppUser> users, AuthMode mode, RoleCatalog roles)
{
    /// <summary>Rola pouzivatela, ktory moze udelit pristup; null = neexistuje alebo je vypnuty.</summary>
    public async Task<string?> RoleOfAsync(string userName)
    {
        // bez prihlasovania je kazdy spravca local - po zapnuti prihlasovania jeho povolenia neplatia
        if (userName == LocalAuthenticationHandler.UserName)
            return mode.LoginRequired ? null : Roles.Admin;
        var user = await users.FindByNameAsync(userName).ConfigureAwait(false);
        if (user is null || user.IsDisabled)
            return null;
        return (await users.GetRolesAsync(user).ConfigureAwait(false)).FirstOrDefault();
    }

    /// <summary>Pouzivatel moze klientovi rolu povolenia stale dat.</summary>
    public async Task<bool> AllowsAsync(OAuthGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        return await RoleOfAsync(grant.UserName).ConfigureAwait(false) is { } role && Covers(role, grant.Role);
    }

    /// <summary>Rola <paramref name="role" /> pokryva vsetky opravnenia roly <paramref name="granted" />.</summary>
    public bool Covers(string role, string granted) => roles.Find(granted) is { } info && roles.Covers(role, info.Permissions);
}
