using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableSimulator.Application.Keys;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>
/// Prihlasenie pristupovym klucom: hlavicka <c>Authorization: Bearer</c> (program aj webove rozhranie zariadenia),
/// pre WebSocket <c>/ws</c> aj parameter <see cref="QueryParameter" /> - prehliadac k WebSocketu hlavicku neposle.
/// Kluc sa hlada podla hashu pri kazdej poziadavke, takze odstraneny kluc prestane platit hned.
/// </summary>
public sealed class AccessKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAccessKeyRepository keys,
    TimeProvider time)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Nazov schemy.</summary>
    public const string SchemeName = "AccessKey";

    /// <summary>Parameter s klucom v adrese WebSocketu a suborov sprav panelov.</summary>
    public const string QueryParameter = "access_token";

    /// <summary>Predpona mena v prihlaseni (a v audite): <c>key:nazov</c>.</summary>
    public const string NamePrefix = "key:";

    private const string Bearer = "Bearer ";

    /// <summary>Kluc z poziadavky; null, ak ho nema.</summary>
    public static string? KeyOf(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var header = request.Headers.Authorization.ToString();
        if (header.StartsWith(Bearer, StringComparison.OrdinalIgnoreCase))
            return header[Bearer.Length..].Trim();
        // WebSocket a obrazky/videa sprav panelov prehliadac nacita bez hlavicky
        return (request.Path.StartsWithSegments("/ws") || request.Path.StartsWithSegments("/api/signage/media")) && request.Query[QueryParameter] is { Count: > 0 } query
            ? query[0]
            : null;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (KeyOf(Request) is not { Length: > 0 } secret)
            return AuthenticateResult.NoResult();
        var key = await keys.FindByHashAsync(AccessKeys.Hash(secret), Context.RequestAborted).ConfigureAwait(false);
        if (key is null)
        {
            AuthLog.KeyRejected(Logger, secret.Length > AccessKeys.PrefixLength ? secret[..AccessKeys.PrefixLength] : "?");
            return AuthenticateResult.Fail("Invalid access key.");
        }

        var now = time.GetUtcNow().UtcDateTime;
        if (key.LastUsedAt is not { } used || now - used > AccessKeys.TouchInterval)
            await keys.TouchAsync(key.Id, now, Context.RequestAborted).ConfigureAwait(false);

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, NamePrefix + key.Name),
                new Claim(AuthClaims.DisplayName, key.Name),
                new Claim(ClaimTypes.Role, key.Role),
                new Claim(AuthClaims.AccessKey, key.Id.ToString("D"))
            ],
            SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
