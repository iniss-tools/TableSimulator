using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Designs;

/// <summary>Zmaze vlastny vzhlad; ak prekryval vstavany, tabule sa vratia k vstavanemu.</summary>
public sealed record DeleteDesignCommand(string Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
