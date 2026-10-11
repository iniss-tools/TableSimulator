using FluentValidation;

namespace TableSimulator.Application.Station;

public sealed class ImportInissStationCommandValidator : AbstractValidator<ImportInissStationCommand>
{
    public ImportInissStationCommandValidator() =>
        RuleFor(c => c.Options!.FaceLanguage).IsInEnum().WithErrorCode("station.face").When(c => c.Options is not null);
}
