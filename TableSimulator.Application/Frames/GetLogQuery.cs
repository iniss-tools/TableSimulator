using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Frames;

namespace TableSimulator.Application.Frames;

/// <summary>Zaznamy komunikacie s poradovym cislom vacsim nez <paramref name="After" />.</summary>
public sealed record GetLogQuery(long After) : IQuery<FrameDto[]>;
