using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>PUT /api/signage/playlists/{id} - zmeni playlist.</summary>
public sealed class UpdateSignagePlaylist : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("signage/playlists/{id:int}", async (int id, SetSignagePlaylistRequest request, ICommandHandler<SaveSignagePlaylistCommand, SignageCreatedDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SaveSignagePlaylistCommand(id, request), cancellationToken)).Match(_ => Results.NoContent(), CustomResults.Problem))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(Permissions.SimulatorEdit);
}
