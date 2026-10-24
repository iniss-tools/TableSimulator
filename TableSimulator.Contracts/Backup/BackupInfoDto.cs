namespace TableSimulator.Contracts.Backup;

/// <summary>Zaloha simulatora - co obsahuje.</summary>
/// <param name="Created">kedy vznikla</param>
/// <param name="ServerVersion">verzia servera, ktory ju vytvoril</param>
/// <param name="Designs">pocet vlastnych vzhladov</param>
/// <param name="Media">pocet suborov informacnych panelov</param>
/// <param name="Recordings">pocet zaznamov komunikacie; null = zaloha zaznamy neobsahuje</param>
public sealed record BackupInfoDto(DateTimeOffset Created, string ServerVersion, int Designs, int Media, int? Recordings);
