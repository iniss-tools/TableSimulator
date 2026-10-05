using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Lines;

/// <summary>Odstrani linku aj s jej tabulami a zastavi jej spojenie.</summary>
public sealed record DeleteLineCommand(int Number) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Number.ToString(CultureInfo.InvariantCulture);
}
