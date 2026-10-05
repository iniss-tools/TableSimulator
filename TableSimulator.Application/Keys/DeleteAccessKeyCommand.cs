using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Keys;

/// <summary>Odstrani pristupovy kluc - prestane platit hned.</summary>
public sealed record DeleteAccessKeyCommand(Guid Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString("D", CultureInfo.InvariantCulture);
}
