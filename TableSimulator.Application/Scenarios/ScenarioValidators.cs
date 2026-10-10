using FluentValidation;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Application.Scenarios;

/// <summary>Scenar porucha: nazov, kroky a ich casy, ciel a porucha.</summary>
public sealed class FaultScenarioRequestValidator : AbstractValidator<FaultScenarioRequest>
{
    public FaultScenarioRequestValidator()
    {
        RuleFor(r => r.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(ScenarioErrors.Name);
        RuleFor(r => r.CycleMs).InclusiveBetween(1000, ScenarioErrors.MaxMs).When(r => r.CycleMs is not null).WithErrorCode(ScenarioErrors.Cycle);
        RuleFor(r => r.Steps).NotNull().Must(s => s.Count <= ScenarioErrors.MaxSteps).WithErrorCode(ScenarioErrors.Steps);
        RuleForEach(r => r.Steps).NotNull().WithErrorCode(ScenarioErrors.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.AtMs).InclusiveBetween(0, ScenarioErrors.MaxMs).WithErrorCode(ScenarioErrors.StepTime);
            step.RuleFor(s => s.DurationMs).InclusiveBetween(ScenarioErrors.MinDurationMs, ScenarioErrors.MaxMs).WithErrorCode(ScenarioErrors.StepTime);
            step.RuleFor(s => s.Line).InclusiveBetween(1, 100).WithErrorCode(ScenarioErrors.StepLine);
            step.RuleFor(s => s.Address).InclusiveBetween(0, 255).When(s => s.Address is not null).WithErrorCode(ScenarioErrors.StepAddress);
            step.RuleFor(s => s.Fault).NotNull().When(s => s.Action == ScenarioAction.Fault).WithErrorCode(ScenarioErrors.StepFault)
                .WithMessage("A fault step needs a fault.");
            step.RuleFor(s => s.Fault!.Kind).IsInEnum().NotEqual(FaultKind.None).When(s => s.Action == ScenarioAction.Fault && s.Fault is not null)
                .WithErrorCode(ScenarioErrors.StepFault);
            step.RuleFor(s => s.Fault!.DelayMs).InclusiveBetween(0, 60_000).When(s => s.Fault is not null).WithErrorCode(ScenarioErrors.StepFault);
            step.RuleFor(s => s.Fault!.Status).InclusiveBetween(0, 255).When(s => s.Fault is not null).WithErrorCode(ScenarioErrors.StepFault);
        });
    }
}

public sealed class CreateFaultScenarioCommandValidator : AbstractValidator<CreateFaultScenarioCommand>
{
    public CreateFaultScenarioCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(ScenarioErrors.Name).SetValidator(new FaultScenarioRequestValidator());
    }
}

public sealed class SetFaultScenarioCommandValidator : AbstractValidator<SetFaultScenarioCommand>
{
    public SetFaultScenarioCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(ScenarioErrors.Name).SetValidator(new FaultScenarioRequestValidator());
    }
}
