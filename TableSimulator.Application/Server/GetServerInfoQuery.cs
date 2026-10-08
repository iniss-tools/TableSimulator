using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Server;

namespace TableSimulator.Application.Server;

/// <summary>Ako server bezi (sluzba Windows, verzia, priecinok nastaveni).</summary>
public sealed record GetServerInfoQuery : IQuery<ServerInfoDto>;
