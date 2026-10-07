using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Audit;

namespace TableSimulator.Api.Audit;

/// <summary>GET /api/audit?before=&amp;user=&amp;action=&amp;limit= - audit od najnovsich zaznamov.</summary>
public sealed class GetAudit : IEndpoint
{
    private const int DefaultLimit = 100;

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("audit", async (long? before, string? user, string? action, int? limit, IQueryHandler<GetAuditQuery, AuditPageDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetAuditQuery(before, user, action, limit ?? DefaultLimit), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<AuditPageDto>()
            .RequireAuthorization(Permissions.AuditView);
}
