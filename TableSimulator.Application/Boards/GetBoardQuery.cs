using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Tabula podla identifikatora <c>linka-adresa</c>.</summary>
public sealed record GetBoardQuery(string Id) : IQuery<BoardDto>;
