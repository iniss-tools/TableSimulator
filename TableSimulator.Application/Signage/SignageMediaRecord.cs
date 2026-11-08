namespace TableSimulator.Application.Signage;

/// <summary>Nahraty subor spravy; obsah je v priecinku <c>media</c> pod menom <paramref name="FileName" />.</summary>
public sealed record SignageMediaRecord(Guid Id, string Name, string ContentType, long Size, string FileName, DateTimeOffset CreatedAt);
