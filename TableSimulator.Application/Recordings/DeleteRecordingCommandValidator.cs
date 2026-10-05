using FluentValidation;

namespace TableSimulator.Application.Recordings;

public sealed class DeleteRecordingCommandValidator : AbstractValidator<DeleteRecordingCommand>
{
    public DeleteRecordingCommandValidator() => RuleFor(c => c.Name).Must(RecordingNames.IsValid).WithErrorCode(RecordingErrors.Name);
}
