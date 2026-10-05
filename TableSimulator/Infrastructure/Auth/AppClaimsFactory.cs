using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Prihlasenie nesie aj meno pre zobrazenie.</summary>
public sealed class AppClaimsFactory(UserManager<AppUser> users, RoleManager<AppRole> roles, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser, AppRole>(users, roles, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var identity = await base.GenerateClaimsAsync(user).ConfigureAwait(false);
        identity.AddClaim(new Claim(AuthClaims.DisplayName, string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "" : user.DisplayName));
        return identity;
    }
}
