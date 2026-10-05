using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Api.Users;

/// <summary>PUT /api/users/{userName} - meno, rola a vypnutie pouzivatela.</summary>
public sealed class UpdateUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("users/{userName}", async (string userName, UpdateUserRequest request, ICommandHandler<UpdateUserCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new UpdateUserCommand(userName, request), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .RequireAuthorization(Permissions.UsersManage);
}
