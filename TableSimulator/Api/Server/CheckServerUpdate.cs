using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Server;
using TableSimulator.Contracts.Server;

namespace TableSimulator.Api.Server;

/// <summary>POST /api/server/update/check - zisti novu verziu servera hned.</summary>
public sealed class CheckServerUpdate : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("server/update/check", async (ICommandHandler<CheckServerUpdateCommand, ServerUpdateDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new CheckServerUpdateCommand(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<ServerUpdateDto>()
            .RequireAuthorization(Policies.ServerView);
}
