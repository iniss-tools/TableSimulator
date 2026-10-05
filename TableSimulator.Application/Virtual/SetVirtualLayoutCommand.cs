using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Application.Virtual;

/// <summary>Zalozi alebo zmeni vlastne rozlozenie s klucom <paramref name="Id" />.</summary>
public sealed record SetVirtualLayoutCommand(string Id, VirtualLayoutDto Layout) : ICommand;
