using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Faces;

namespace TableSimulator.Application.Faces;

/// <summary>Predlohy ciel tabul - vstavane a vlastne.</summary>
public sealed record GetFacesQuery : IQuery<BoardFaceDto[]>;
