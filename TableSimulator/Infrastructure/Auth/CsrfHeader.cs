namespace TableSimulator.Infrastructure.Auth;

/// <summary>
/// Ochrana proti CSRF: poziadavky, ktore menia stav (<c>/api</c> mimo GET), musia niest hlavicku
/// <see cref="HeaderName" />. Cudzia stranka ju bez povolenia CORS poslat nevie - prehliadac by sa najprv pytal servera.
/// Poziadavka s pristupovym klucom v hlavicke <c>Authorization</c> ju nepotrebuje - tu prehliadac sam neprida.
/// </summary>
public static class CsrfHeader
{
    /// <summary>Hlavicka, ktoru posiela webove rozhranie aj GVDEditor.</summary>
    public const string HeaderName = "X-Requested-With";

    public static WebApplication UseCsrfHeader(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.Use(async (context, next) =>
        {
            var request = context.Request;
            if (request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)
                && !HttpMethods.IsOptions(request.Method) && !request.Headers.ContainsKey(HeaderName)
                && !request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                await Results.Problem(
                    title: "Forbidden",
                    detail: $"State-changing requests must carry the {HeaderName} header.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: new Dictionary<string, object?> { ["code"] = "auth.csrf" }).ExecuteAsync(context);
                return;
            }

            await next(context);
        });
        return app;
    }
}
