using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Api.Users;

/// <summary>POST /api/users - novy pouzivatel.</summary>
public sealed class CreateUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("users", async (CreateUserRequest request, ICommandHandler<CreateUserCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new CreateUserCommand(request), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .RequireAuthorization(Permissions.UsersManage);
}
