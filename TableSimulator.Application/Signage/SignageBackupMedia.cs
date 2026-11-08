namespace TableSimulator.Application.Signage;

/// <summary>Nahraty subor v zalohe; obsah je v ZIP pod <paramref name="File" />.</summary>
public sealed record SignageBackupMedia(Guid Id, string Name, string ContentType, long Size, string File);
