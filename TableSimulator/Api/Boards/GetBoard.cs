using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Api.Boards;

/// <summary>GET /api/boards/{id} - tabula.</summary>
public sealed class GetBoard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("boards/{id}", async (string id, IQueryHandler<GetBoardQuery, BoardDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetBoardQuery(id), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<BoardDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
