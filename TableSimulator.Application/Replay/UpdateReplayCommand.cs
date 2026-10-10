using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Recordings;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Replay;

/// <summary>Zmeni prebiehajuce prehravanie (pauza, posun, rychlost, opakovanie).</summary>
public sealed record UpdateReplayCommand(UpdateReplayRequest Request) : ICommand<ReplayStatusDto>, IAuditable
{
    string IAuditable.AuditTarget => "replay";
}

public sealed class UpdateReplayCommandValidator : AbstractValidator<UpdateReplayCommand>
{
    public UpdateReplayCommandValidator()
    {
        RuleFor(c => c.Request.Speed).InclusiveBetween(0, 1000).When(c => c.Request.Speed is not null).WithErrorCode(RecordingErrors.Speed);
        RuleFor(c => c.Request.Position).GreaterThanOrEqualTo(0).When(c => c.Request.Position is not null).WithErrorCode(RecordingErrors.Time);
    }
}

public sealed class UpdateReplayCommandHandler(RecordingService recordings) : ICommandHandler<UpdateReplayCommand, ReplayStatusDto>
{
    public async Task<Result<ReplayStatusDto>> Handle(UpdateReplayCommand command, CancellationToken cancellationToken) =>
        await recordings.UpdateReplayAsync(command.Request).ConfigureAwait(false) is { } status
            ? Result.Success(status)
            : Result.Failure<ReplayStatusDto>(RecordingErrors.NoReplay());
}
