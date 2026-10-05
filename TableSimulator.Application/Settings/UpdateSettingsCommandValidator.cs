using FluentValidation;

namespace TableSimulator.Application.Settings;

public sealed class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(SettingsErrors.Port);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.HttpPort).InclusiveBetween(1, 65535).WithErrorCode(SettingsErrors.Port);
            RuleFor(c => c.Request.HttpsPort).InclusiveBetween(0, 65535).WithErrorCode(SettingsErrors.HttpsPort);
            RuleFor(c => c.Request.HttpsPort).NotEqual(c => c.Request.HttpPort).When(c => c.Request.HttpsPort > 0).WithErrorCode(SettingsErrors.HttpsPort)
                .WithMessage("The HTTPS port must differ from the HTTP port.");
            RuleFor(c => c.Request.ConnectHost).Must(h => h is null || !h.Trim().Any(char.IsWhiteSpace)).WithErrorCode(SettingsErrors.ConnectHost)
                .WithMessage("The address must not contain spaces.");
            RuleFor(c => c.Request.VirtualFolders)
                .Must(f => f is null || f.Count(p => !string.IsNullOrWhiteSpace(p)) <= SettingsErrors.MaxVirtualFolders)
                .WithErrorCode(SettingsErrors.VirtualFolders)
                .WithMessage($"At most {SettingsErrors.MaxVirtualFolders} folders with table XML files.");
        });
    }
}
