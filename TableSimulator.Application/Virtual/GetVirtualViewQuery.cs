using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Application.Virtual;

/// <summary>Co ukazuje virtualna tabula <paramref name="Id" />: vybrane zaznamy jej XML.</summary>
public sealed record GetVirtualViewQuery(int Id) : IQuery<VirtualViewDto>;
