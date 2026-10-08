using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Signage;

/// <summary>Odstrani spravu, playlist, skupinu alebo panel - len ak ich nic nepouziva.</summary>
public sealed record DeleteSignageCommand(SignageEntity Entity, int Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => string.Create(CultureInfo.InvariantCulture, $"{SignageErrors.Key(Entity)} {Id}");
}

public sealed class DeleteSignageCommandHandler(ISignageRepository repository, SignageMediaStore store, ILiveNotifier live) : ICommandHandler<DeleteSignageCommand>
{
    public async Task<Result> Handle(DeleteSignageCommand command, CancellationToken cancellationToken)
    {
        var data = SignageData.From(await repository.LoadAsync(cancellationToken).ConfigureAwait(false));
        var users = UsersOf(data, command.Entity, command.Id).ToList();
        if (users.Count > 0)
            return Result.Failure(SignageErrors.InUse(command.Entity, users));
        if (!await repository.DeleteAsync(command.Entity, command.Id, cancellationToken).ConfigureAwait(false))
            return Result.Failure(SignageErrors.NotFound(command.Entity, command.Id));

        // subor odstranenej spravy, ak ho ina sprava nepouziva
        if (command.Entity == SignageEntity.Message && data.Messages.GetValueOrDefault(command.Id)?.Definition.Media is { } media
                                                    && !data.Messages.Values.Any(m => m.Id != command.Id && m.Definition.Media == media)
                                                    && data.Media.TryGetValue(media, out var file))
        {
            await repository.DeleteMediaAsync([media], cancellationToken).ConfigureAwait(false);
            store.Delete([file]);
        }

        live.Publish(new SignageChangedMessage());
        return Result.Success();
    }

    /// <summary>Co entitu pouziva (nazvy pre hlasenie).</summary>
    private static IEnumerable<string> UsersOf(SignageData data, SignageEntity entity, int id) => entity switch
    {
        SignageEntity.Message => data.PlaylistsWith(id).Select(p => p.Definition.Name)
            .Concat(data.Groups.Values.Where(g => g.Definition.DefaultMessage == id).Select(g => g.Definition.Name)),
        SignageEntity.Playlist => data.Groups.Values
            .Where(g => g.Definition.Playlists.Contains(id) || g.Definition.DefaultPlaylist == id).Select(g => g.Definition.Name),
        SignageEntity.Group => data.Panels.Values.Where(p => p.GroupId == id).Select(p => p.Name),
        _ => []
    };
}
