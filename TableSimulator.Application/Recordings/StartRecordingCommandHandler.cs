using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Recordings;

public sealed class StartRecordingCommandHandler(RecordingService recordings) : ICommandHandler<StartRecordingCommand, RecorderStatusDto>
{
    public async Task<Result<RecorderStatusDto>> Handle(StartRecordingCommand command, CancellationToken cancellationToken) =>
        Result.Success(await recordings.StartAsync(command.Note).ConfigureAwait(false));
}
