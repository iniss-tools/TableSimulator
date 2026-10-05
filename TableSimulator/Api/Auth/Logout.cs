using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;

namespace TableSimulator.Api.Auth;

/// <summary>POST /api/auth/logout - odhlasenie.</summary>
public sealed class Logout : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("auth/logout", async (ICommandHandler<LogoutCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new LogoutCommand(), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .AllowAnonymous();
}
