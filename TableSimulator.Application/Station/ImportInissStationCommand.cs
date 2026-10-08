using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Application.Station;

/// <summary>Nacita instalaciu INISSu z nastavenia a zalozi jej linky a tabule (ako import stanice z GVDEditora).</summary>
public sealed record ImportInissStationCommand : ICommand<StationImportedDto>;
