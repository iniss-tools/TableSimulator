using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Prida tabulu alebo zmeni tabulu s linkou a adresou z adresy poziadavky.</summary>
public sealed record SetBoardCommand(int Line, int Address, SetBoardRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => string.Create(CultureInfo.InvariantCulture, $"{Line}-{Address}");
}
