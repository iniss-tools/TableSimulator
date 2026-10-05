namespace TableSimulator.Application.Recordings;

/// <summary>Subor zaznamu na stiahnutie.</summary>
/// <param name="Path">cesta k suboru</param>
/// <param name="DownloadName">meno suboru pre prehliadac</param>
public sealed record RecordingFile(string Path, string DownloadName);
