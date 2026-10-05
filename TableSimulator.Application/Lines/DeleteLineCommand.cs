using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Lines;

/// <summary>Odstrani linku aj s jej tabulami a zastavi jej spojenie.</summary>
public sealed record DeleteLineCommand(int Number) : ICommand;
