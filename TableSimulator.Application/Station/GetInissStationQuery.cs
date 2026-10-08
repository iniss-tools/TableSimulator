using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Application.Station;

/// <summary>Linky a tabule instalacie INISSu z nastavenia (nahlad pred nacitanim do simulatora).</summary>
public sealed record GetInissStationQuery : IQuery<InissStationDto>;
