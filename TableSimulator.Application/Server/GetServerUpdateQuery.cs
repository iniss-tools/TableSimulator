using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Server;

namespace TableSimulator.Application.Server;

/// <summary>Posledne zistena nova verzia servera.</summary>
public sealed record GetServerUpdateQuery : IQuery<ServerUpdateDto>;
