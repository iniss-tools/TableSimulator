using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Virtual;

public sealed class DeleteVirtualLayoutCommandHandler(IVirtualLayoutRepository layouts, IVirtualBoardRepository boards, ILiveNotifier live)
    : ICommandHandler<DeleteVirtualLayoutCommand>
{
    public async Task<Result> Handle(DeleteVirtualLayoutCommand command, CancellationToken cancellationToken)
    {
        if (VirtualLayouts.FindBuiltIn(command.Id) is not null)
            return Result.Failure(VirtualErrors.LayoutBuiltIn(command.Id));
        var users = (await boards.ListAsync(cancellationToken).ConfigureAwait(false))
            .Where(b => string.Equals(b.Layout, command.Id, StringComparison.OrdinalIgnoreCase)).Select(b => b.Id).ToArray();
        if (users.Length > 0)
            return Result.Failure(VirtualErrors.LayoutInUse(command.Id, users));
        if (!await layouts.DeleteAsync(command.Id, cancellationToken).ConfigureAwait(false))
            return Result.Failure(VirtualErrors.LayoutNotFound(command.Id));
        live.Publish(new VirtualMessage([], true));
        return Result.Success();
    }
}
