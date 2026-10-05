using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Audit;

namespace TableSimulator.Application.Audit;

/// <summary>Stranka auditu od najnovsich zaznamov.</summary>
/// <param name="Before">len starsie nez zaznam s tymto Id; null = od najnovsieho</param>
/// <param name="User">len pouzivatel; null = vsetci</param>
/// <param name="Action">len akcia; null = vsetky</param>
/// <param name="Limit">pocet zaznamov na stranke</param>
public sealed record GetAuditQuery(long? Before, string? User, string? Action, int Limit) : IQuery<AuditPageDto>;
