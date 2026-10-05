using FluentValidation;
using TableSimulator.Application.Recordings;

namespace TableSimulator.Application.Replay;

public sealed class PlayRecordingCommandValidator : AbstractValidator<PlayRecordingCommand>
{
    public PlayRecordingCommandValidator()
    {
        RuleFor(c => c.Name).Must(RecordingNames.IsValid).WithErrorCode(RecordingErrors.Name);
        RuleFor(c => c.Speed).InclusiveBetween(0, 1000).WithErrorCode(RecordingErrors.Speed);
    }
}
