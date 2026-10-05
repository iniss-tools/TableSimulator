using FluentValidation;

namespace TableSimulator.Application.Recordings;

public sealed class StartRecordingCommandValidator : AbstractValidator<StartRecordingCommand>
{
    public StartRecordingCommandValidator() => RuleFor(c => c.Note).MaximumLength(500).WithErrorCode(RecordingErrors.Note);
}
