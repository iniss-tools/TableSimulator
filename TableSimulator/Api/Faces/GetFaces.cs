using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Faces;
using TableSimulator.Contracts.Faces;

namespace TableSimulator.Api.Faces;

/// <summary>GET /api/faces - predlohy ciel tabul (vstavane a vlastne).</summary>
[UsedImplicitly]
public sealed class GetFaces : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("faces", async (IQueryHandler<GetFacesQuery, BoardFaceDto[]> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetFacesQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<BoardFaceDto[]>()
            .RequireAuthorization(Permissions.SimulatorView);
}
