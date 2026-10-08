using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Application.Designs;

/// <summary>Ulozi vlastny vzhlad (rovnake id prekryje vstavany) - tabule s nim sa prekreslia hned.</summary>
public sealed record SetDesignCommand(string Id, SetDesignRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
