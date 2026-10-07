using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Api.Users;

/// <summary>GET /api/users - pouzivatelia.</summary>
public sealed class GetUsers : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("users", async (IQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetUsersQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<IReadOnlyList<UserDto>>()
            .RequireAuthorization(Permissions.UsersManage);
}
