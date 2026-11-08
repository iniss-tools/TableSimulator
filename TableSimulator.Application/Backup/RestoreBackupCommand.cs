using System.Text.Json.Serialization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Backup;

namespace TableSimulator.Application.Backup;

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
