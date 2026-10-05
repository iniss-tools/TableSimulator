using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Auth;

namespace TableSimulator.Infrastructure.Realtime;

/// <summary>WebSocket zivych zmien pre prehliadace.</summary>
public static class LiveSocketEndpoint
{
    /// <summary>
    /// Spojenie <paramref name="pattern" />; <c>?log=1</c> = aj zaznamy komunikacie (s opravnenim). Spojenie z cudzej
    /// stranky (hlavicka Origin s inym serverom) sa odmietne - prehliadac by inak priniesol prihlasovaciu cookie.
    /// </summary>
    public static IEndpointConventionBuilder MapLiveSocket(this IEndpointRouteBuilder app, string pattern) =>
        app.Map(pattern, async (HttpContext context, LiveHub hub, IAuthorizationService authorization) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return Results.BadRequest();
            if (!SameOrigin(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var wantsLog = context.Request.Query["log"] == "1";
            if (wantsLog && !(await authorization.AuthorizeAsync(context.User, Permissions.LogView)).Succeeded)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await hub.ServeAsync(socket, wantsLog, context.RequestAborted);
            return Results.Empty;
        }).RequireAuthorization(Permissions.SimulatorView);

    private static bool SameOrigin(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        return origin.Length == 0
               || Uri.TryCreate(origin, UriKind.Absolute, out var uri) && string.Equals(uri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }
}
