using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Virtual;

/// <summary>Odstrani vlastne rozlozenie, ktore ziadna tabula nepouziva.</summary>
public sealed record DeleteVirtualLayoutCommand(string Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
