using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Server;
using TableSimulator.Contracts.Backup;

namespace TableSimulator.Application.Backup;

/// <summary>Stav zalohy a obnovy.</summary>
public sealed record GetBackupStatusQuery : IQuery<BackupStatusDto>;

public sealed class GetBackupStatusQueryHandler(IServerBackup backup, IServerHost host) : IQueryHandler<GetBackupStatusQuery, BackupStatusDto>
{
    public Task<Result<BackupStatusDto>> Handle(GetBackupStatusQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new BackupStatusDto(backup.PendingRestore(), host.RunningAsService, backup.LastRestore())));
}
