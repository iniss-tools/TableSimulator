namespace TableSimulator.Contracts.Keys;

/// <summary>Pristupovy kluc bez tajnej casti (GET /api/keys).</summary>
/// <param name="Id">identifikator</param>
/// <param name="Name">nazov (v audite <c>key:nazov</c>)</param>
/// <param name="Kind">druh</param>
/// <param name="Role">rola, s ktorou kluc pristupuje</param>
/// <param name="Prefix">zaciatok kluca na rozlisenie</param>
/// <param name="CreatedAt">kedy vznikol</param>
/// <param name="CreatedBy">kto ho vytvoril</param>
/// <param name="LastUsedAt">posledne pouzitie (s presnostou na minutu); null = este nikdy</param>
public sealed record AccessKeyDto(
    Guid Id,
    string Name,
    AccessKeyKind Kind,
    string Role,
    string Prefix,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? LastUsedAt);
