using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableSimulator.Application.Auth;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>
/// Rezim bez prihlasenia: server pocuva len na tomto pocitaci a kazda poziadavka je spravca <c>local</c>. Politiky
/// opravneni tak plati rovnako v oboch rezimoch.
/// </summary>
public sealed class LocalAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Nazov schemy.</summary>
    public const string SchemeName = "Local";

    /// <summary>Prihlasovacie meno pouzivatela v rezime bez prihlasenia.</summary>
    public const string UserName = "local";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, UserName), new Claim(ClaimTypes.Role, Roles.Admin), new Claim(AuthClaims.DisplayName, UserName)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
