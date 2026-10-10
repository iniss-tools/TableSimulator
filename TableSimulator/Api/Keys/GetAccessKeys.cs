using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Keys;
using TableSimulator.Contracts.Keys;

namespace TableSimulator.Api.Keys;

/// <summary>GET /api/keys - pristupove kluce bez tajnych hodnot.</summary>
public sealed class GetAccessKeys : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("keys", async (IQueryHandler<GetAccessKeysQuery, IReadOnlyList<AccessKeyDto>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetAccessKeysQuery(), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<IReadOnlyList<AccessKeyDto>>()
            .RequireAuthorization(Permissions.KeysManage);
}
