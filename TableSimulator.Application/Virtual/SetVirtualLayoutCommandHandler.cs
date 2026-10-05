using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Virtual;

public sealed class SetVirtualLayoutCommandHandler(IVirtualLayoutRepository layouts, ILiveNotifier live) : ICommandHandler<SetVirtualLayoutCommand>
{
    public async Task<Result> Handle(SetVirtualLayoutCommand command, CancellationToken cancellationToken)
    {
        if (VirtualLayouts.FindBuiltIn(command.Id) is not null)
            return Result.Failure(VirtualErrors.LayoutBuiltIn(command.Id));

        var layout = command.Layout with { Id = command.Id, Name = command.Layout.Name.Trim() };
        await layouts.SaveAsync(new VirtualLayoutRecord(layout.Id, layout.Name, VirtualLayouts.Serialize(layout)), cancellationToken).ConfigureAwait(false);
        live.Publish(new VirtualMessage([], true));
        return Result.Success();
    }
}
