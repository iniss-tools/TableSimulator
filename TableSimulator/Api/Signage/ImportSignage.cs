using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>POST /api/signage/import?replace= - obnova zalohy informacnych panelov (telo = ZIP).</summary>
public sealed class ImportSignage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("signage/import", async (HttpRequest request, bool? replace, ICommandHandler<ImportSignageCommand, SignageImportedDto> handler,
                CancellationToken cancellationToken) =>
            {
                // zaloha s videami je vacsia nez predvoleny limit tela poziadavky; vacsiu zastavi az handler
                if (request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
                    limit.MaxRequestBodySize = ImportSignageCommandHandler.MaxBytes + 1;
                var result = await handler.Handle(new ImportSignageCommand(replace ?? false, request.Body), cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .Accepts<Stream>("application/octet-stream")
            .Produces<SignageImportedDto>()
            .RequireAuthorization(Permissions.SimulatorEdit);
}
