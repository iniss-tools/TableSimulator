using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.State;

namespace TableSimulator.Application.State;

/// <summary>Cely stav simulatora pre prve nacitanie stranky.</summary>
public sealed record GetStateQuery : IQuery<StateDto>;
