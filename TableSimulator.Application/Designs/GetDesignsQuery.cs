using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Application.Designs;

/// <summary>Vzhlady tabul.</summary>
public sealed record GetDesignsQuery : IQuery<DesignInfoDto[]>;
