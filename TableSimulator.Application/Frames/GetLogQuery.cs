using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Frames;

namespace TableSimulator.Application.Frames;

/// <summary>Zaznamy komunikacie s poradovym cislom vacsim nez <paramref name="After" />.</summary>
public sealed record GetLogQuery(long After) : IQuery<FrameDto[]>;
