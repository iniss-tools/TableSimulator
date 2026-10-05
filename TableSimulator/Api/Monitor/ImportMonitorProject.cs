using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Monitor;
using TableSimulator.Contracts.Monitor;

namespace TableSimulator.Api.Monitor;

/// <summary>POST /api/monitor/import - tabule z projektu ELEN Monitora.</summary>
public sealed class ImportMonitorProject : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("monitor/import", async (MonitorImportRequest request, ICommandHandler<ImportMonitorProjectCommand, MonitorImportedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new ImportMonitorProjectCommand(request.Project, request.Line), cancellationToken))
            .Match(Results.Ok, CustomResults.Problem));
}
