using System.Globalization;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Zalozi (<paramref name="Id" /> null) alebo zmeni playlist.</summary>
public sealed record SaveSignagePlaylistCommand(int? Id, SetSignagePlaylistRequest Request) : ICommand<SignageCreatedDto>, IAuditable
{
    string IAuditable.AuditTarget => Id?.ToString(CultureInfo.InvariantCulture) ?? Request.Name;
}

public sealed class SaveSignagePlaylistCommandValidator : AbstractValidator<SaveSignagePlaylistCommand>
{
    public SaveSignagePlaylistCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).When(c => c.Id is not null).WithErrorCode(SignageErrors.Id);
        RuleFor(c => c.Request).NotNull().WithErrorCode(SignageErrors.Name).SetValidator(new SignagePlaylistRules());
    }
}

public sealed class SaveSignagePlaylistCommandHandler(ISignageRepository repository, ILiveNotifier live)
    : ICommandHandler<SaveSignagePlaylistCommand, SignageCreatedDto>
{
    public async Task<Result<SignageCreatedDto>> Handle(SaveSignagePlaylistCommand command, CancellationToken cancellationToken)
    {
        var r = command.Request;
        var request = r with
        {
            Name = r.Name.Trim(),
            End = r.Period == SignagePeriod.Once ? null : r.End,
            Weekdays = r.Period == SignagePeriod.Weekly ? [.. r.Weekdays.Distinct().Order()] : []
        };
        var data = SignageData.From(await repository.LoadAsync(cancellationToken).ConfigureAwait(false));
        var missing = request.Items.Select(i => i.Message).Where(m => !data.Messages.ContainsKey(m)).Distinct().ToList();
        if (missing.Count > 0)
            return Result.Failure<SignageCreatedDto>(SignageErrors.UnknownMessages(missing));

        var id = await repository.SavePlaylistAsync(new SignagePlaylistRecord(command.Id ?? 0, request.Name, SignageJson.Serialize(request)), cancellationToken)
            .ConfigureAwait(false);
        if (id == 0)
            return Result.Failure<SignageCreatedDto>(SignageErrors.NotFound(SignageEntity.Playlist, command.Id ?? 0));
        live.Publish(new SignageChangedMessage());
        return Result.Success(new SignageCreatedDto(id));
    }
}
