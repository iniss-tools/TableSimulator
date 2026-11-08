using FluentValidation;

namespace TableSimulator.Application.Boards;

public sealed class SetAkSignalCommandValidator : AbstractValidator<SetAkSignalCommand>
{
    public SetAkSignalCommandValidator()
    {
        RuleFor(c => c.Settings).NotNull().WithErrorCode(BoardErrors.AkSignal);
        RuleFor(c => c.Settings.Conditions).NotNull().WithErrorCode(BoardErrors.AkSignal).When(c => c.Settings is not null);
        RuleFor(c => c.Settings)
            .Must(s => AkSignalRules.IsValid(s.Sensor, s.SoundSeconds, s.SyncSeconds, s.SilenceSeconds))
            .When(c => c.Settings is not null).WithErrorCode(BoardErrors.AkSignal).WithMessage(AkSignalRules.Message);
    }
}
