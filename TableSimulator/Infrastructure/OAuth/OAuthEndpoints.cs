using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Authentication;
using TableSimulator.Application.Auth;
using TableSimulator.Application.OAuth;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Infrastructure.Mcp;

namespace TableSimulator.Infrastructure.OAuth;

/// <summary>
/// Server OAuth 2.1 pre klientov MCP - opis servera (RFC 8414), dynamicka registracia klienta (RFC 7591), povolenie
/// s PKCE S256 (rozhoduje pouzivatel na stranke webu <c>#/authorize/{id}</c> cez <c>/api/oauth</c>) a tokeny s obnovovanim.
/// </summary>
public static class OAuthEndpoints
{
    public const string AuthorizePath = "/oauth/authorize";
    public const string TokenPath = "/oauth/token";
    public const string RegisterPath = "/oauth/register";

    /// <summary>Stranka povolenia pri neplatnej ziadosti (neznamy klient, cudzia adresa navratu).</summary>
    public const string InvalidRequest = "invalid";

    /// <summary>Schema vyzvy 401 s odkazom na opis chraneneho zdroja (len vyzva, neprihlasuje).</summary>
    public const string ChallengeScheme = "McpAuth";

    private static readonly string[] ResponseTypes = ["code"];
    private static readonly string[] GrantTypes = ["authorization_code", "refresh_token"];
    private static readonly string[] ChallengeMethods = ["S256"];
    private static readonly string[] AuthMethods = ["none"];

    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public static IServiceCollection AddOAuth(this IServiceCollection services)
    {
        services.AddSingleton<OAuthPending>();
        services.AddScoped<OAuthUsers>();
        services.AddScoped<OAuthServer>();
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, OAuthTokenAuthenticationHandler>(OAuthTokenAuthenticationHandler.SchemeName, null)
            // opis chraneneho zdroja /.well-known/oauth-protected-resource/mcp a vyzva 401 s odkazom nan
            .AddMcp(ChallengeScheme, "MCP", options =>
            {
                // predvolene preposiela overenie schemy "Bearer" (JWT) - tu overuje OAuthTokenAuthenticationHandler
                options.ForwardAuthenticate = null;
                options.ResourceMetadata = new ProtectedResourceMetadata { ResourceName = "TableSimulator", BearerMethodsSupported = ["header"] };
                options.Events.OnResourceMetadataRequest = context =>
                {
                    context.ResourceMetadata!.AuthorizationServers = [Issuer(context.HttpContext.Request)];
                    return Task.CompletedTask;
                };
            });
        return services;
    }

    public static void MapOAuth(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/oauth-authorization-server", (HttpRequest request) => Results.Json(new
        {
            issuer = Issuer(request),
            authorization_endpoint = Issuer(request) + AuthorizePath,
            token_endpoint = Issuer(request) + TokenPath,
            registration_endpoint = Issuer(request) + RegisterPath,
            response_types_supported = ResponseTypes,
            grant_types_supported = GrantTypes,
            code_challenge_methods_supported = ChallengeMethods,
            token_endpoint_auth_methods_supported = AuthMethods,
            // navrat nesie aj vydavatela (RFC 9207) - klient odhali zamenu serverov
            authorization_response_iss_parameter_supported = true
        })).AllowAnonymous().ExcludeFromDescription();

        app.MapPost(RegisterPath, Register).AllowAnonymous().ExcludeFromDescription();
        app.MapGet(AuthorizePath, Authorize).AllowAnonymous().ExcludeFromDescription();
        app.MapPost(TokenPath, Token).AllowAnonymous().DisableAntiforgery().ExcludeFromDescription();
    }

    /// <summary>Adresa servera = vydavatel tokenov (rovnaka ako adresa MCP bez cesty).</summary>
    public static string Issuer(HttpRequest request) => $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase}";

    private sealed record RegistrationRequest(
        [property: JsonPropertyName("client_name")] string? ClientName,
        [property: JsonPropertyName("redirect_uris")] string[]? RedirectUris);

    private static async Task<IResult> Register(HttpRequest request, OAuthServer server)
    {
        RegistrationRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<RegistrationRequest>(request.Body, cancellationToken: request.HttpContext.RequestAborted);
        }
        catch (JsonException)
        {
            return Error("invalid_client_metadata", "The body is not JSON.");
        }
        var uris = body?.RedirectUris ?? [];
        if (uris.Length == 0 || !uris.All(IsAllowedRedirect))
            return Error("invalid_redirect_uri", "Redirect URIs must be https, http on this computer (loopback) or an application scheme.");
        var name = string.IsNullOrWhiteSpace(body!.ClientName) ? "MCP client" : body.ClientName.Trim();
        var client = await server.RegisterAsync(name.Length > 100 ? name[..100] : name, uris, request.HttpContext.RequestAborted);
        return Results.Json(new
        {
            client_id = client.Id,
            client_name = client.Name,
            redirect_uris = client.RedirectUris,
            client_id_issued_at = new DateTimeOffset(client.CreatedAt).ToUnixTimeSeconds(),
            token_endpoint_auth_method = "none",
            grant_types = GrantTypes,
            response_types = ResponseTypes
        }, statusCode: StatusCodes.Status201Created);
    }

    /// <summary>
    /// Overi klienta a adresu navratu a presmeruje na stranku povolenia webu (<c>#/authorize/{id}</c>) - tam sa pouzivatel
    /// prihlasi a rozhodne. Neznamy klient alebo cudzia adresa navratu = stranka s chybou, nikam inam sa nepresmeruje.
    /// </summary>
    private static async Task<IResult> Authorize(HttpContext context, OAuthServer server, OAuthPending pending)
    {
        var query = context.Request.Query;
        var web = context.Request.PathBase + "/#/authorize/";
        var client = await server.FindClientAsync(query["client_id"], context.RequestAborted);
        string? redirect = query["redirect_uri"];
        if (client is null || redirect is null || !client.RedirectUris.Any(r => SameRedirect(r, redirect)))
            return Results.Redirect(web + InvalidRequest);
        string? state = query["state"];
        if (query["response_type"] != "code" || query["code_challenge_method"] != "S256" || string.IsNullOrEmpty(query["code_challenge"]))
            return Results.Redirect(OAuthRedirect.With(redirect, ("error", "invalid_request"), ("state", state), ("iss", Issuer(context.Request))));
        var request = pending.CreateRequest(client, redirect, query["code_challenge"]!, state, Issuer(context.Request));
        return Results.Redirect(web + request.Id);
    }

    private static async Task<IResult> Token(HttpContext context, OAuthServer server)
    {
        if (!context.Request.HasFormContentType)
            return Error("invalid_request", "Expected application/x-www-form-urlencoded.");
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var (tokens, error) = form["grant_type"].ToString() switch
        {
            "authorization_code" => await server.ExchangeCodeAsync(form["code"], form["client_id"], form["redirect_uri"], form["code_verifier"],
                context.RequestAborted),
            "refresh_token" => await server.RefreshAsync(form["refresh_token"], form["client_id"], context.RequestAborted),
            _ => ((OAuthTokens?)null, "unsupported_grant_type")
        };
        context.Response.Headers.CacheControl = "no-store";
        if (tokens is null)
            return Error(error!, null);
        return Results.Json(new
        {
            access_token = tokens.AccessToken,
            token_type = "Bearer",
            expires_in = (int)tokens.ExpiresIn.TotalSeconds,
            refresh_token = tokens.RefreshToken
        });
    }

    /// <summary>Prihlaseny pouzivatel (cookie alebo rezim bez prihlasovania); kluc povolenie udelit nemoze.</summary>
    /// <summary>Navrat: https, aplikacna schema alebo http na tomto pocitaci (RFC 8252).</summary>
    private static bool IsAllowedRedirect(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.Fragment) && uri.Scheme switch
        {
            "https" => true,
            "http" => uri.IsLoopback,
            "javascript" or "data" or "file" or "vbscript" or "about" or "blob" or "ftp" or "ws" or "wss" => false,
            _ => true
        };

    /// <summary>Rovnaka adresa navratu; na tomto pocitaci (loopback) s lubovolnym portom (RFC 8252).</summary>
    private static bool SameRedirect(string registered, string requested)
    {
        if (registered == requested)
            return true;
        return Uri.TryCreate(registered, UriKind.Absolute, out var a) && Uri.TryCreate(requested, UriKind.Absolute, out var b)
               && a is { Scheme: "http", IsLoopback: true } && b.Scheme == "http" && a.Host == b.Host && a.PathAndQuery == b.PathAndQuery;
    }

    private static IResult Error(string error, string? description) =>
        Results.Json(new { error, error_description = description }, Json, statusCode: StatusCodes.Status400BadRequest);

}
