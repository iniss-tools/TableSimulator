using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Virtual;

/// <summary>Odstrani virtualnu tabulu.</summary>
public sealed record DeleteVirtualBoardCommand(int Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id.ToString(CultureInfo.InvariantCulture);
}
