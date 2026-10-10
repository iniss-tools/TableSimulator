using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Keys;
using TableSimulator.Contracts.Keys;

namespace TableSimulator.Api.Keys;

/// <summary>POST /api/keys - novy pristupovy kluc (tajna hodnota len v odpovedi).</summary>
public sealed class CreateAccessKey : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("keys", async (CreateAccessKeyRequest request, ICommandHandler<CreateAccessKeyCommand, AccessKeyCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new CreateAccessKeyCommand(request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<AccessKeyCreatedDto>()
            .RequireAuthorization(Permissions.KeysManage);
}
