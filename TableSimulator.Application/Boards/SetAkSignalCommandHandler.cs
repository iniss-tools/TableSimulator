using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Core.Boards.AkSignal;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

public sealed class SetAkSignalCommandHandler(Simulator simulator) : ICommandHandler<SetAkSignalCommand>
{
    public Task<Result> Handle(SetAkSignalCommand command, CancellationToken cancellationToken)
    {
        var kind = simulator.Read(s => s.FindBoard(command.Id)?.Display);
        if (kind is null)
            return Task.FromResult(Result.Failure(BoardErrors.NotFound(command.Id)));
        if (kind is not AkSignalDisplay)
            return Task.FromResult(Result.Failure(BoardErrors.NotAkSignal(command.Id)));

        // stav ustredne sa prejavi pri dalsom dotaze servera aj tiku - tik ho prepocita hned
        simulator.UpdateProfile(command.Id, p => p with { AkSignal = RequestMapper.WithSettings(p.AkSignal ?? AkSignalProfile.Default, command.Settings) });
        simulator.Tick();
        return Task.FromResult(Result.Success());
    }
}
