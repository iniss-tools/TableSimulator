using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

public sealed class StopRecordingCommandHandler(RecordingService recordings) : ICommandHandler<StopRecordingCommand, RecorderStatusDto>
{
    public async Task<Result<RecorderStatusDto>> Handle(StopRecordingCommand command, CancellationToken cancellationToken) =>
        Result.Success(await recordings.StopAsync().ConfigureAwait(false));
}
