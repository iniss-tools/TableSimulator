using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Faces;

public sealed class DeleteFaceCommandHandler(IFaceRepository faces, Simulator simulator, ILiveNotifier live) : ICommandHandler<DeleteFaceCommand>
{
    public async Task<Result> Handle(DeleteFaceCommand command, CancellationToken cancellationToken)
    {
        if (BoardFaces.FindBuiltIn(command.Id) is not null)
            return Result.Failure(FaceErrors.BuiltIn(command.Id));
        var users = simulator.Read(s => s.Boards.Where(b => string.Equals(b.Profile.Face, command.Id, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Id).ToArray());
        if (users.Length > 0)
            return Result.Failure(FaceErrors.InUse(command.Id, users));
        if (!await faces.DeleteAsync(command.Id, cancellationToken).ConfigureAwait(false))
            return Result.Failure(FaceErrors.NotFound(command.Id));
        live.Publish(new FacesMessage());
        return Result.Success();
    }
}
