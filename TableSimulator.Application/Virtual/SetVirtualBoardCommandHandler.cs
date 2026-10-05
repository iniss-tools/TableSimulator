using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Virtual;

public sealed class SetVirtualBoardCommandHandler(IVirtualBoardRepository boards, ILiveNotifier live) : ICommandHandler<SetVirtualBoardCommand>
{
    public async Task<Result> Handle(SetVirtualBoardCommand command, CancellationToken cancellationToken)
    {
        await boards.SaveAsync(RequestMapper.ToVirtualBoard(command.Id, command.Request), cancellationToken).ConfigureAwait(false);
        live.Publish(new VirtualMessage([], true));
        return Result.Success();
    }
}
