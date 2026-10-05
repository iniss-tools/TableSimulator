using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Keys;

namespace TableSimulator.Application.Keys;

/// <summary>Vytvori pristupovy kluc; tajna hodnota je len vo vysledku (do auditu nejde).</summary>
public sealed record CreateAccessKeyCommand(CreateAccessKeyRequest Request) : ICommand<AccessKeyCreatedDto>, IAuditable
{
    string IAuditable.AuditTarget => Request.Name;
}
