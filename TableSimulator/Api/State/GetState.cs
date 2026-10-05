using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.State;
using TableSimulator.Contracts.State;

namespace TableSimulator.Api.State;

/// <summary>GET /api/state - cely stav pre prve nacitanie stranky.</summary>
public sealed class GetState : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("state", async (IQueryHandler<GetStateQuery, StateDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetStateQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem));
}
