using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Backup;

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
