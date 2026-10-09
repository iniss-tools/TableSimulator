using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Recordings;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Api.Recordings;

/// <summary>GET /api/recordings/compare?a=&amp;b= - co ukazovali tabule v dvoch zaznamoch.</summary>
public sealed class CompareRecordings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("recordings/compare", async (string a, string b, IQueryHandler<CompareRecordingsQuery, RecordingComparisonDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new CompareRecordingsQuery(a, b), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<RecordingComparisonDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
