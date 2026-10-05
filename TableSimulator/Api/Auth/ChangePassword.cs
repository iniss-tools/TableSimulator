using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Api.Auth;

/// <summary>POST /api/auth/password - zmena vlastneho hesla (kazdy prihlaseny).</summary>
public sealed class ChangePassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("auth/password", async (ChangePasswordRequest request, ICommandHandler<ChangePasswordCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new ChangePasswordCommand(request), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .RequireAuthorization(Permissions.SimulatorView);
}
