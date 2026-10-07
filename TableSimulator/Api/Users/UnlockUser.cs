using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;

namespace TableSimulator.Api.Users;

/// <summary>POST /api/users/{userName}/unlock - zrusi zablokovanie po neuspesnych pokusoch.</summary>
public sealed class UnlockUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("users/{userName}/unlock", async (string userName, ICommandHandler<UnlockUserCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new UnlockUserCommand(userName), cancellationToken)).Match(Results.NoContent, CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.UsersManage);
}
