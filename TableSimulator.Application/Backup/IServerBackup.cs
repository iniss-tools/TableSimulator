using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Backup;

namespace TableSimulator.Application.Backup;

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
