using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Replay;

/// <summary>Prehra zaznam do liniek simulatora.</summary>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Loop">dokola</param>
public sealed record PlayRecordingCommand(string Name, double Speed, bool Loop) : ICommand<ReplayStatusDto>;
