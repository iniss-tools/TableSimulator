namespace TableSimulator.Contracts.Audit;

/// <summary>Stranka auditu od najnovsich zaznamov (GET /api/audit).</summary>
/// <param name="Entries">zaznamy od najnovsieho</param>
/// <param name="HasMore">su aj starsie zaznamy (dalsia stranka: <c>before</c> = Id posledneho)</param>
public sealed record AuditPageDto(IReadOnlyList<AuditEntryDto> Entries, bool HasMore);
