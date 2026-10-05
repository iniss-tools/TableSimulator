using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Recordings;

namespace TableSimulator.Api.Recordings;

/// <summary>DELETE /api/recordings/{name} - odstrani zaznam.</summary>
public sealed class DeleteRecording : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("recordings/{name}", async (string name, ICommandHandler<DeleteRecordingCommand> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteRecordingCommand(name), cancellationToken)).Match(Results.NoContent, CustomResults.Problem));
}
