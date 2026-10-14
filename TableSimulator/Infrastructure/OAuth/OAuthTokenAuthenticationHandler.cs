using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableSimulator.Application.OAuth;
using TableSimulator.Infrastructure.Auth;

namespace TableSimulator.Infrastructure.OAuth;

/// <summary>
/// Prihlasenie tokenom OAuth (len server MCP): <c>Authorization: Bearer tso_…</c> = povolenie pouzivatela pre klienta
/// s rolou z povolenia. Neplatny alebo prepadnuty token = 401 a klient ho obnovi.
/// </summary>
public sealed class OAuthTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    OAuthServer server)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "OAuthToken";

    /// <summary>Predpona mena v prihlaseni (a v audite): <c>mcp:pouzivatel/klient</c>.</summary>
    public const string NamePrefix = "mcp:";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (AccessKeyAuthenticationHandler.KeyOf(Request) is not { } token || !OAuthSecrets.IsAccessToken(token))
            return AuthenticateResult.NoResult();
        if (await server.AuthenticateAsync(token, Context.RequestAborted).ConfigureAwait(false) is not { } grant)
            return AuthenticateResult.Fail("Invalid or expired token.");

        var name = $"{NamePrefix}{grant.UserName}/{grant.ClientName}";
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, name),
                new Claim(AuthClaims.DisplayName, name),
                new Claim(ClaimTypes.Role, grant.Role)
            ],
            SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
