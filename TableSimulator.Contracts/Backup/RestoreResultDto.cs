namespace TableSimulator.Contracts.Backup;

/// <summary>Posledna obnova zalohy pri starte servera (pri sluzbe Windows inak nie je vidiet, ako dopadla).</summary>
/// <param name="At">kedy sa obnovovalo</param>
/// <param name="Backup">obnovena zaloha</param>
/// <param name="Folder">kam sa odlozili povodne udaje; pri chybe kde ostala zaloha</param>
/// <param name="Error">chyba; null = obnova sa podarila</param>
public sealed record RestoreResultDto(DateTimeOffset At, BackupInfoDto Backup, string Folder, string? Error);
