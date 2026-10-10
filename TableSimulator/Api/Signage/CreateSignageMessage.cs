using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>POST /api/signage/messages - zalozi spravu; vrati cislo.</summary>
public sealed class CreateSignageMessage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("signage/messages", async (SetSignageMessageRequest request, ICommandHandler<SaveSignageMessageCommand, SignageCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SaveSignageMessageCommand(null, request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<SignageCreatedDto>()
            .RequireAuthorization(Permissions.SignageManage);
}
