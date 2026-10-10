using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;
using TableSimulator.Contracts.Roles;

namespace TableSimulator.Api.Roles;

/// <summary>GET /api/roles - vsetky roly s opravneniami a poctom pouzivatelov a klucov (aj pre spravu klucov).</summary>
public sealed class GetRoles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("roles", async (IQueryHandler<GetRolesQuery, IReadOnlyList<RoleDto>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetRolesQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<IReadOnlyList<RoleDto>>()
            .RequireAuthorization(Policies.RolesView);
}
