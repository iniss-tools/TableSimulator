using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Auth;

namespace TableSimulator.Application.Auth;

/// <summary>Prihlaseny pouzivatel a rezim prihlasenia.</summary>
public sealed record GetMeQuery : IQuery<MeDto>;
