using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Recordings;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Replay;

public sealed class PlayRecordingCommandHandler(RecordingService recordings) : ICommandHandler<PlayRecordingCommand, ReplayStatusDto>
{
    public async Task<Result<ReplayStatusDto>> Handle(PlayRecordingCommand command, CancellationToken cancellationToken) =>
        await recordings.PlayAsync(command.Name, command.Speed, command.Loop).ConfigureAwait(false) is { } status
            ? Result.Success(status)
            : Result.Failure<ReplayStatusDto>(RecordingErrors.NotFound(command.Name));
}
