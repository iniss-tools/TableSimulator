using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Recordings;

namespace TableSimulator.Application.Replay;

public sealed class StopReplayCommandHandler(RecordingService recordings) : ICommandHandler<StopReplayCommand>
{
    public async Task<Result> Handle(StopReplayCommand command, CancellationToken cancellationToken)
    {
        await recordings.StopReplayAsync().ConfigureAwait(false);
        return Result.Success();
    }
}
