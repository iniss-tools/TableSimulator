using FluentValidation;

namespace TableSimulator.Application.Recordings;

public sealed class UploadRecordingCommandValidator : AbstractValidator<UploadRecordingCommand>
{
    public UploadRecordingCommandValidator() => RuleFor(c => c.Content).NotEmpty().WithErrorCode(RecordingErrors.Empty);
}
