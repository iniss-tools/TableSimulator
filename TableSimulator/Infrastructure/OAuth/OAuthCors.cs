namespace TableSimulator.Infrastructure.OAuth;

/// <summary>
/// CORS pre verejne casti servera OAuth - opisy servera a zdroja, registracia klienta a tokeny. Klient MCP ich moze
/// volat z prehliadaca (VS Code - agent v okne) a bez hlaviciek CORS by odpoved zahodil. Adresy nepracuju s cookie
/// a nic chranene nevracaju, preto kazdy povod; <c>/mcp</c> ani stranka povolenia CORS nemaju.
/// </summary>
public static class OAuthCors
{
    /// <summary>Verejna cast servera OAuth, ktoru klient vola cez fetch.</summary>
    public static bool IsPublic(PathString path) =>
        path.StartsWithSegments("/.well-known/oauth-authorization-server") || path.StartsWithSegments("/.well-known/oauth-protected-resource")
        || path.StartsWithSegments(OAuthEndpoints.RegisterPath) || path.StartsWithSegments(OAuthEndpoints.TokenPath);

    /// <summary>Hlavicky CORS a odpoved na predbeznu poziadavku (OPTIONS) - pred prihlasenim, ktore opis zdroja vracia.</summary>
    public static IApplicationBuilder UseOAuthCors(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            if (!IsPublic(context.Request.Path) || context.Request.Headers.Origin.Count == 0)
                return next(context);
            var headers = context.Response.Headers;
            headers.AccessControlAllowOrigin = "*";
            if (!HttpMethods.IsOptions(context.Request.Method))
                return next(context);
            headers.AccessControlAllowMethods = "GET, POST, OPTIONS";
            headers.AccessControlAllowHeaders = context.Request.Headers.AccessControlRequestHeaders.Count > 0
                ? context.Request.Headers.AccessControlRequestHeaders.ToString()
                : "Content-Type";
            headers.AccessControlMaxAge = "600";
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
}
