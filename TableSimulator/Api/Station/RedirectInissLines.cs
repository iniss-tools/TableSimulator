using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Api.Station;

/// <summary>POST /api/station/iniss/redirect - presmeruje linky servera INISS2 na simulator alebo presmerovanie zrusi.</summary>
public sealed class RedirectInissLines : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("station/iniss/redirect", async (InissRedirectRequest request, ICommandHandler<RedirectInissLinesCommand, InissStationDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new RedirectInissLinesCommand(request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<InissStationDto>()
            .RequireAuthorization(Permissions.LinesManage);
}
