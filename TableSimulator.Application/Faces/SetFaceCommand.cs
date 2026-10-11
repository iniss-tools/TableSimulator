using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Faces;

namespace TableSimulator.Application.Faces;

/// <summary>Zalozi alebo zmeni vlastnu predlohu cela s klucom <paramref name="Id" />.</summary>
public sealed record SetFaceCommand(string Id, BoardFaceDto Face) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Id;
}
