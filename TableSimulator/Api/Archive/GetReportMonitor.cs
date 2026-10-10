using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Archive;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Archive;

namespace TableSimulator.Api.Archive;

/// <summary>GET /api/archive/reports/live?limit= - monitor hlaseni: najnovsie zaznamy sledovaneho archivu hlaseni.</summary>
public sealed class GetReportMonitor : IEndpoint
{
    private const int DefaultLimit = 500;

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("archive/reports/live", async (int? limit, IQueryHandler<GetReportMonitorQuery, ReportMonitorDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetReportMonitorQuery(limit ?? DefaultLimit), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<ReportMonitorDto>()
            .RequireAuthorization(Permissions.ArchiveView);
}
