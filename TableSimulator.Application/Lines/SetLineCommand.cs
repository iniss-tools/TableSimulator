using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Lines;

namespace TableSimulator.Application.Lines;

/// <summary>Prida linku alebo zmeni linku s cislom <paramref name="Number" /> a spusti jej spojenie.</summary>
public sealed record SetLineCommand(int Number, SetLineRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Number.ToString(CultureInfo.InvariantCulture);
}
