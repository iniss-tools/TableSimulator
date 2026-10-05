using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Frames;
using TableSimulator.Contracts.Frames;

namespace TableSimulator.Api.Frames;

/// <summary>GET /api/log?after= - zaznamy komunikacie.</summary>
public sealed class GetLog : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("log", async (long? after, IQueryHandler<GetLogQuery, FrameDto[]> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetLogQuery(after ?? 0), cancellationToken)).Match(Results.Ok, CustomResults.Problem));
}
