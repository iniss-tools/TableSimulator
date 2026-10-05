using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Application.Station;

/// <summary>Zalozi linky a tabule stanice INISSu (GVDEditor pri presmerovani liniek na simulator).</summary>
public sealed record ImportStationCommand(StationImportRequest Request) : ICommand<StationImportedDto>;
