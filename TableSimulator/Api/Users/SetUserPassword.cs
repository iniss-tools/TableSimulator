using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Api.Users;

/// <summary>POST /api/users/{userName}/password - nove heslo pouzivatela od spravcu.</summary>
public sealed class SetUserPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("users/{userName}/password", async (string userName, SetPasswordRequest request, ICommandHandler<SetUserPasswordCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetUserPasswordCommand(userName, request), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.UsersManage);
}
