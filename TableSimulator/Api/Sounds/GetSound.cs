using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Devices.Sounds;

namespace TableSimulator.Api.Sounds;

/// <summary>
/// GET /api/sounds?name=… - nahravka banky zvukov programu INISSu z nastavenia ako WAV (prehratie hlasenia
/// v prehliadaci).
/// </summary>
public sealed class GetSound : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("sounds", async (string name, HttpContext context, IQueryHandler<GetSoundQuery, byte[]> handler, CancellationToken cancellationToken) =>
                (await handler.Handle(new GetSoundQuery(name), cancellationToken)).Match(wav =>
                {
                    // hlasenie opakuje rovnake nahravky - banka sa meni zriedka
                    context.Response.Headers[HeaderNames.CacheControl] = "private, max-age=300";
                    return Results.File(wav, "audio/wav");
                }, CustomResults.Problem))
            .Produces(StatusCodes.Status200OK, contentType: "audio/wav")
            .RequireAuthorization(Permissions.SimulatorView);
}
