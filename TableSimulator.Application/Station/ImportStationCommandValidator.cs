using FluentValidation;

namespace TableSimulator.Application.Station;

/// <summary>Cisla liniek mimo 1-100 a nezname triedy nie su chyba - simulator ich vynecha a ohlasi vo vysledku.</summary>
public sealed class ImportStationCommandValidator : AbstractValidator<ImportStationCommand>
{
    public ImportStationCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode("station.request");
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.Lines).NotNull().WithErrorCode("station.request");
            RuleFor(c => c.Request.Boards).NotNull().WithErrorCode("station.request");
            RuleForEach(c => c.Request.Lines).ChildRules(line =>
                line.RuleFor(l => l.Port).InclusiveBetween(1, 65535).WithErrorCode("station.port"));
            RuleForEach(c => c.Request.Boards).ChildRules(board =>
            {
                board.RuleFor(b => b.Address).InclusiveBetween(0, 255).WithErrorCode("station.address");
                board.RuleFor(b => b.Manufacturer).NotEmpty().WithErrorCode("station.manufacturer");
            });
        });
    }
}
