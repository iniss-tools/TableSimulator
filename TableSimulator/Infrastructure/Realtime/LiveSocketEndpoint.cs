using Microsoft.AspNetCore.Routing;

namespace TableSimulator.Infrastructure.Realtime;

/// <summary>WebSocket zivych zmien pre prehliadace.</summary>
public static class LiveSocketEndpoint
{
    /// <summary>Spojenie <paramref name="pattern" />; <c>?log=1</c> = aj zaznamy komunikacie.</summary>
    public static IEndpointConventionBuilder MapLiveSocket(this IEndpointRouteBuilder app, string pattern) =>
        app.Map(pattern, async (HttpContext context, LiveHub hub) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return Results.BadRequest();
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await hub.ServeAsync(socket, context.Request.Query["log"] == "1", context.RequestAborted);
            return Results.Empty;
        });
}
