using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Keys;

namespace TableSimulator.Application.Keys;

/// <summary>Pristupove kluce bez tajnych hodnot.</summary>
public sealed record GetAccessKeysQuery : IQuery<IReadOnlyList<AccessKeyDto>>;
