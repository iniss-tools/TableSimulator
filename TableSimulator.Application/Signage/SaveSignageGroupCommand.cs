using System.Globalization;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Zalozi (<paramref name="Id" /> null) alebo zmeni skupinu panelov.</summary>
public sealed record SaveSignageGroupCommand(int? Id, SetSignageGroupRequest Request) : ICommand<SignageCreatedDto>, IAuditable
{
    string IAuditable.AuditTarget => Id?.ToString(CultureInfo.InvariantCulture) ?? Request.Name;
}

public sealed class SaveSignageGroupCommandValidator : AbstractValidator<SaveSignageGroupCommand>
{
    public SaveSignageGroupCommandValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0).When(c => c.Id is not null).WithErrorCode(SignageErrors.Id);
        RuleFor(c => c.Request).NotNull().WithErrorCode(SignageErrors.Name).SetValidator(new SignageGroupRules());
    }
}

public sealed class SaveSignageGroupCommandHandler(ISignageRepository repository, ILiveNotifier live) : ICommandHandler<SaveSignageGroupCommand, SignageCreatedDto>
{
    public async Task<Result<SignageCreatedDto>> Handle(SaveSignageGroupCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request with { Name = command.Request.Name.Trim(), Note = command.Request.Note.Trim() };
        var data = SignageData.From(await repository.LoadAsync(cancellationToken).ConfigureAwait(false));
        var playlists = request.Playlists.Concat(request.DefaultPlaylist is { } d ? [d] : []);
        var missing = playlists.Where(p => !data.Playlists.ContainsKey(p)).Distinct().ToList();
        if (missing.Count > 0)
            return Result.Failure<SignageCreatedDto>(SignageErrors.UnknownPlaylists(missing));
        if (request.DefaultMessage is { } message && !data.Messages.ContainsKey(message))
            return Result.Failure<SignageCreatedDto>(SignageErrors.UnknownMessages([message]));

        var id = await repository.SaveGroupAsync(new SignageGroupRecord(command.Id ?? 0, request.Name, SignageJson.Serialize(request)), cancellationToken)
            .ConfigureAwait(false);
        if (id == 0)
            return Result.Failure<SignageCreatedDto>(SignageErrors.NotFound(SignageEntity.Group, command.Id ?? 0));
        live.Publish(new SignageChangedMessage());
        return Result.Success(new SignageCreatedDto(id));
    }
}
