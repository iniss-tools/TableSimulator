using FluentValidation;

namespace TableSimulator.Application.Recordings;

public sealed class GetRecordingFileQueryValidator : AbstractValidator<GetRecordingFileQuery>
{
    public GetRecordingFileQueryValidator() => RuleFor(q => q.Name).Must(RecordingNames.IsValid).WithErrorCode(RecordingErrors.Name);
}
