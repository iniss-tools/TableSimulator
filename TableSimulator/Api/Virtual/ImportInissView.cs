using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Api.Virtual;

/// <summary>POST /api/virtual/import - virtualne tabule z definicii v subore Web.config INISSView.</summary>
public sealed class ImportInissView : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("virtual/import", async (InissViewImportRequest request, ICommandHandler<ImportInissViewCommand, InissViewImportedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new ImportInissViewCommand(request.Content, request.Replace), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<InissViewImportedDto>()
            .RequireAuthorization(Permissions.SimulatorEdit);
}
