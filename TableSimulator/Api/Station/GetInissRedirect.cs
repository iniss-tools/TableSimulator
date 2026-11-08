using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Api.Station;

/// <summary>GET /api/station/iniss/redirect - stav presmerovania liniek servera INISS2 z nastavenia.</summary>
public sealed class GetInissRedirect : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("station/iniss/redirect", async (IQueryHandler<GetInissRedirectQuery, InissRedirectDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetInissRedirectQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<InissRedirectDto>()
            .RequireAuthorization(Permissions.LinesManage);
}
