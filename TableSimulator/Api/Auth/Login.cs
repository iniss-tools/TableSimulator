using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Auth;

namespace TableSimulator.Api.Auth;

/// <summary>POST /api/auth/login - prihlasenie menom a heslom (cookie).</summary>
public sealed class Login : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("auth/login", async (LoginRequest request, ICommandHandler<LoginCommand, MeDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new LoginCommand(request.UserName ?? "", request.Password ?? "", request.Remember), cancellationToken))
            .Match(Results.Ok, CustomResults.Problem))
            .Produces<MeDto>()
            .AllowAnonymous();
}
