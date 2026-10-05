using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Monitor;

namespace TableSimulator.Application.Monitor;

/// <summary>Instalacia ELEN Monitora: predlohy tabul, projekty a vzhlad.</summary>
public sealed record GetMonitorQuery : IQuery<MonitorDto>;
