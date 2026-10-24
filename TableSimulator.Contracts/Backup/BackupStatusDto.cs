namespace TableSimulator.Contracts.Backup;

/// <summary>Stav zalohy a obnovy (GET /api/backup).</summary>
/// <param name="Pending">obnova pripravena na najblizsi start servera; null = ziadna</param>
/// <param name="Service">server bezi ako sluzba Windows - da sa restartovat z webu</param>
/// <param name="LastRestore">posledna obnova pri starte servera; null = este ziadna</param>
public sealed record BackupStatusDto(BackupInfoDto? Pending, bool Service, RestoreResultDto? LastRestore);
