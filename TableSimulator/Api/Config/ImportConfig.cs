using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Config;
using TableSimulator.Contracts.Config;

namespace TableSimulator.Api.Config;

/// <summary>POST /api/config/import - linky a tabule zo suboru (pridat chybajuce alebo nahradit vsetky).</summary>
public sealed class ImportConfig : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("config/import", async (ImportConfigRequest request, ICommandHandler<ImportConfigCommand, ConfigImportedDto> handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new ImportConfigCommand(request.Content ?? "", request.Replace), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<ConfigImportedDto>()
            .RequireAuthorization(Permissions.SimulatorEdit);
}
