using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Pwa;
using TableSimulator.Contracts.Pwa;

namespace TableSimulator.Api.Pwa;

/// <summary>
/// GET /api/manifest?view=…&amp;name=…&amp;key=… - manifest aplikacie kiosku. Bez prihlasenia: prehliadac manifest
/// stahuje bez cookie; kluc zariadenia prisiel od zariadenia samotneho a vracia sa len v start_url.
/// </summary>
public sealed class GetWebManifest : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("manifest", async (string view, string? name, string? key, IQueryHandler<GetWebManifestQuery, WebManifestDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetWebManifestQuery(view, name, key), cancellationToken))
            .Match(manifest => Results.Json(manifest, contentType: "application/manifest+json"), CustomResults.Problem))
            .Produces<WebManifestDto>(contentType: "application/manifest+json")
            .AllowAnonymous();
}
