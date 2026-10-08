using System.Globalization;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Zalozi (<paramref name="Id" /> null) alebo zmeni panel.</summary>
public sealed record SaveSignagePanelCommand(int? Id, SetSignagePanelRequest Request) : ICommand<SignageCreatedDto>, IAuditable
{
    string IAuditable.AuditTarget => Id?.ToString(CultureInfo.InvariantCulture) ?? Request.Name;
}

public sealed class SaveSignagePanelCommandValidator : AbstractValidator<SaveSignagePanelCommand>
{
    public SaveSignagePanelCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).When(c => c.Id is not null).WithErrorCode(SignageErrors.Id);
        RuleFor(c => c.Request).NotNull().WithErrorCode(SignageErrors.Name).SetValidator(new SignagePanelRules());
    }
}

public sealed class SaveSignagePanelCommandHandler(ISignageRepository repository, ILiveNotifier live) : ICommandHandler<SaveSignagePanelCommand, SignageCreatedDto>
{
    public async Task<Result<SignageCreatedDto>> Handle(SaveSignagePanelCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (request.Group is { } group)
        {
            var data = SignageData.From(await repository.LoadAsync(cancellationToken).ConfigureAwait(false));
            if (!data.Groups.ContainsKey(group))
                return Result.Failure<SignageCreatedDto>(SignageErrors.UnknownGroup(group));
        }

        var id = await repository.SavePanelAsync(new SignagePanelRecord(command.Id ?? 0, request.Name.Trim(), request.Note.Trim(), request.Group),
            cancellationToken).ConfigureAwait(false);
        if (id == 0)
            return Result.Failure<SignageCreatedDto>(SignageErrors.NotFound(SignageEntity.Panel, command.Id ?? 0));
        live.Publish(new SignageChangedMessage());
        return Result.Success(new SignageCreatedDto(id));
    }
}
