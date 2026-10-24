using System.Text.Json.Serialization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Server;
using TableSimulator.Contracts.Backup;

namespace TableSimulator.Application.Backup;

/// <summary>Hotova zaloha v docasnom subore (zmaze sa po odoslani).</summary>
/// <param name="Path">docasny subor</param>
/// <param name="FileName">meno pri stiahnuti</param>
public sealed record BackupFile(string Path, string FileName);

/// <summary>
/// Zaloha a obnova celeho simulatora (implementuje server): databaza, vlastne vzhlady, subory informacnych panelov
/// a volitelne zaznamy komunikacie. Obnova sa len pripravi a pouzije sa pri najblizsom starte servera.
/// </summary>
public interface IServerBackup
{
    /// <summary>Vytvori zalohu (ZIP) v docasnom subore.</summary>
    Task<BackupFile> CreateAsync(bool recordings, CancellationToken cancellationToken);

    /// <summary>Skontroluje zalohu a pripravi obnovu na najblizsi start servera (predchadzajucu pripravenu nahradi).</summary>
    Task<Result<BackupInfoDto>> PrepareRestoreAsync(Stream content, CancellationToken cancellationToken);

    /// <summary>Pripravena obnova; null = ziadna.</summary>
    BackupInfoDto? PendingRestore();

    /// <summary>Posledna obnova pri starte servera; null = este ziadna.</summary>
    RestoreResultDto? LastRestore();

    /// <summary>Zrusi pripravenu obnovu; false = ziadna nebola.</summary>
    bool CancelRestore();
}

/// <summary>Chyby zalohy a obnovy.</summary>
public static class BackupErrors
{
    /// <summary>Najvacsia zaloha na obnovu.</summary>
    public const long MaxBytes = 8L * 1024 * 1024 * 1024;

    public static Error Format(string detail) => Error.Problem("backup.format", $"The file is not a simulator backup: {detail}");

    public static Error Newer(string migration) =>
        Error.Problem("backup.newer", $"The backup comes from a newer server version (database migration {migration}) - update the server first.");

    public static Error TooLarge() => Error.Problem("backup.tooLarge", "The backup is larger than 8 GB.");

    public static Error NoPending() => Error.NotFound("backup.noPending", "No restore is prepared.");
}

/// <summary>Stav zalohy a obnovy.</summary>
public sealed record GetBackupStatusQuery : IQuery<BackupStatusDto>;

public sealed class GetBackupStatusQueryHandler(IServerBackup backup, IServerHost host) : IQueryHandler<GetBackupStatusQuery, BackupStatusDto>
{
    public Task<Result<BackupStatusDto>> Handle(GetBackupStatusQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new BackupStatusDto(backup.PendingRestore(), host.RunningAsService, backup.LastRestore())));
}

/// <summary>Zaloha celeho simulatora; <paramref name="Recordings" /> = aj zaznamy komunikacie.</summary>
public sealed record CreateBackupQuery(bool Recordings) : IQuery<BackupFile>;

public sealed class CreateBackupQueryHandler(IServerBackup backup) : IQueryHandler<CreateBackupQuery, BackupFile>
{
    public async Task<Result<BackupFile>> Handle(CreateBackupQuery query, CancellationToken cancellationToken) =>
        Result.Success(await backup.CreateAsync(query.Recordings, cancellationToken).ConfigureAwait(false));
}

/// <summary>Pripravi obnovu zo zalohy na najblizsi start servera. Obsah do auditu nejde.</summary>
public sealed record RestoreBackupCommand([property: JsonIgnore] Stream Content) : ICommand<BackupInfoDto>, IAuditable
{
    string IAuditable.AuditTarget => "restore";
}

public sealed class RestoreBackupCommandHandler(IServerBackup backup) : ICommandHandler<RestoreBackupCommand, BackupInfoDto>
{
    public Task<Result<BackupInfoDto>> Handle(RestoreBackupCommand command, CancellationToken cancellationToken) =>
        backup.PrepareRestoreAsync(command.Content, cancellationToken);
}

/// <summary>Zrusi pripravenu obnovu.</summary>
public sealed record CancelRestoreCommand : ICommand, IAuditable
{
    string IAuditable.AuditTarget => "restore";
}

public sealed class CancelRestoreCommandHandler(IServerBackup backup) : ICommandHandler<CancelRestoreCommand>
{
    public Task<Result> Handle(CancelRestoreCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(backup.CancelRestore() ? Result.Success() : Result.Failure(BackupErrors.NoPending()));
}
