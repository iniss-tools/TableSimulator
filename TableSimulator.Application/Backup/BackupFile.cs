namespace TableSimulator.Application.Backup;

/// <summary>Hotova zaloha v docasnom subore (zmaze sa po odoslani).</summary>
/// <param name="Path">docasny subor</param>
/// <param name="FileName">meno pri stiahnuti</param>
public sealed record BackupFile(string Path, string FileName);
