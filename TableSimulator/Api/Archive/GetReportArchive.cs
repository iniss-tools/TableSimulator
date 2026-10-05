using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Archive;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Archive;

namespace TableSimulator.Api.Archive;

/// <summary>GET /api/archive/reports/{datum}?train=&amp;text=&amp;newest=&amp;offset=&amp;limit= - stranka archivu jedneho dna.</summary>
public sealed class GetReportArchive : IEndpoint
{
    private const int DefaultLimit = 200;

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("archive/reports/{date}", async (DateOnly date, string? train, string? text, bool? newest, int? offset, int? limit,
            IQueryHandler<GetReportArchiveQuery, ReportArchivePageDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetReportArchiveQuery(date, train, text, newest ?? false, offset ?? 0, limit ?? DefaultLimit), cancellationToken))
            .Match(Results.Ok, CustomResults.Problem))
            .RequireAuthorization(Permissions.SimulatorView);
}
