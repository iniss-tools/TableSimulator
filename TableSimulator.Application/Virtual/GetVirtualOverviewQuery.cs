using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Application.Virtual;

/// <summary>Virtualne tabule, najdene XML tabul a sledovane priecinky.</summary>
public sealed record GetVirtualOverviewQuery : IQuery<VirtualOverviewDto>;
