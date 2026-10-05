using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Virtual;

public sealed class DeleteVirtualBoardCommandHandler(IVirtualBoardRepository boards, ILiveNotifier live) : ICommandHandler<DeleteVirtualBoardCommand>
{
    public async Task<Result> Handle(DeleteVirtualBoardCommand command, CancellationToken cancellationToken)
    {
        if (!await boards.DeleteAsync(command.Id, cancellationToken).ConfigureAwait(false))
            return Result.Failure(VirtualErrors.NotFound(command.Id));
        live.Publish(new VirtualMessage([], true));
        return Result.Success();
    }
}
