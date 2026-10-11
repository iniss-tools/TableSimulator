using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Lines;
using TableSimulator.Application.Mapping;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

public sealed class SetBoardCommandHandler(Simulator simulator, IFaceRepository faces, FaceHeaders headers) : ICommandHandler<SetBoardCommand>
{
    public async Task<Result> Handle(SetBoardCommand command, CancellationToken cancellationToken)
    {
        if (simulator.Read(s => s.FindLine(command.Line)) is null)
            return Result.Failure(LineErrors.NotFound(command.Line));

        var config = RequestMapper.ToConfig(command.Request, command.Line, command.Address);
        if (config.Profile.Face is { } face && BoardFaces.FindBuiltIn(face) is null
                                            && await faces.FindAsync(face, cancellationToken).ConfigureAwait(false) is null)
            return Result.Failure(FaceErrors.NotFound(face));
        // hlavicka (napr. z predlohy ELEN Monitora) sa stane predlohou cela
        config = await headers.AbsorbAsync(config, null, null, cancellationToken).ConfigureAwait(false);

        try
        {
            simulator.SetBoard(config);
            return Result.Success();
        }
        catch (ArgumentException e)
        {
            return Result.Failure(BoardErrors.Invalid(e.Message));
        }
    }
}
