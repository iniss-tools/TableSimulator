using FluentValidation;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Lines;

public sealed class SetLineCommandValidator : AbstractValidator<SetLineCommand>
{
    public SetLineCommandValidator()
    {
        RuleFor(c => c.Number).InclusiveBetween(1, 100).WithErrorCode(LineErrors.Number);
        RuleFor(c => c.Request).NotNull().WithErrorCode(LineErrors.Endpoint);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.TableClass).Must(c => Simulator.SupportedClasses.Contains(c)).WithErrorCode(LineErrors.TableClass)
                .WithMessage("Table class {PropertyValue} is not supported.");
            RuleFor(c => c.Request.Endpoint).Must(e => LineEndpoint.TryParse(e) is not null).WithErrorCode(LineErrors.Endpoint)
                .WithMessage("Endpoint '{PropertyValue}' is not valid.");
            RuleFor(c => c.Request.Mirror).Must(m => string.IsNullOrWhiteSpace(m) || LineEndpoint.TryParse(m) is { Transport: LineTransport.Serial })
                .WithErrorCode(LineErrors.Mirror).WithMessage("Mirror must be a serial port (COMn).");
        });
    }
}
