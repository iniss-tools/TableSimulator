using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>POST /api/signage/playlists - zalozi playlist; vrati cislo.</summary>
public sealed class CreateSignagePlaylist : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("signage/playlists", async (SetSignagePlaylistRequest request, ICommandHandler<SaveSignagePlaylistCommand, SignageCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SaveSignagePlaylistCommand(null, request), cancellationToken)).Match(Results.Ok, CustomResults.Problem))
            .Produces<SignageCreatedDto>()
            .RequireAuthorization(Permissions.SimulatorEdit);
}
