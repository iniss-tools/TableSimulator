using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Server;

namespace TableSimulator.Application.Server;

/// <summary>Zisti novu verziu servera hned (aj ked je pravidelna kontrola v nastaveni vypnuta).</summary>
public sealed record CheckServerUpdateCommand : ICommand<ServerUpdateDto>;
