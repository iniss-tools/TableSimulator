using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Faces;

/// <summary>Odstrani vlastnu predlohu cela, ktoru ziadna tabula nepouziva.</summary>
public sealed record DeleteFaceCommand(string Id) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
