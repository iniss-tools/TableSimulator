using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Faces;

public sealed class SetFaceCommandHandler(IFaceRepository faces, ILiveNotifier live) : ICommandHandler<SetFaceCommand>
{
    public async Task<Result> Handle(SetFaceCommand command, CancellationToken cancellationToken)
    {
        if (BoardFaces.FindBuiltIn(command.Id) is not null)
            return Result.Failure(FaceErrors.BuiltIn(command.Id));

        var face = RequestMapper.ToFace(command.Face, command.Id);
        await faces.SaveAsync(new FaceRecord(face.Id, face.Name, BoardFaces.Serialize(face)), cancellationToken).ConfigureAwait(false);
        live.Publish(new FacesMessage());
        return Result.Success();
    }
}
