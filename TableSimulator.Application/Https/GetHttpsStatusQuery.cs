using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Https;

namespace TableSimulator.Application.Https;

/// <summary>Stav HTTPS servera a jeho certifikatu.</summary>
public sealed record GetHttpsStatusQuery : IQuery<HttpsStatusDto>;
