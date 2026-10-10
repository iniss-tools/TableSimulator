using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>POST /api/signage/groups - zalozi skupinu panelov; vrati cislo.</summary>
public sealed class CreateSignageGroup : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("signage/groups", async (SetSignageGroupRequest request, ICommandHandler<SaveSignageGroupCommand, SignageCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SaveSignageGroupCommand(null, request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<SignageCreatedDto>()
            .RequireAuthorization(Permissions.SignageManage);
}
