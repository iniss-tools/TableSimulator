namespace TableSimulator.Application.Keys;

/// <summary>Pristupovy kluc v databaze - tajna hodnota len ako hash SHA-256.</summary>
/// <param name="Id">identifikator</param>
/// <param name="Name">nazov</param>
/// <param name="Kind">druh (<see cref="AccessKeys.DeviceKind" />, <see cref="AccessKeys.ApiKind" />)</param>
/// <param name="Role">rola</param>
/// <param name="Prefix">zaciatok kluca na rozlisenie</param>
/// <param name="Hash">hash SHA-256 kluca (hex)</param>
/// <param name="CreatedAt">vznik (UTC)</param>
/// <param name="CreatedBy">kto ho vytvoril</param>
/// <param name="LastUsedAt">posledne pouzitie (UTC)</param>
public sealed record AccessKey(
    Guid Id,
    string Name,
    string Kind,
    string Role,
    string Prefix,
    string Hash,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? LastUsedAt);
