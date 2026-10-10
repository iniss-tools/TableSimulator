using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Application.Replay;

/// <summary>Prehra zaznam do liniek simulatora.</summary>
/// <param name="Speed">rychlost; 0 = bez cakania</param>
/// <param name="Loop">dokola</param>
/// <param name="Position">odkial [s]</param>
/// <param name="Paused">len pripravit a pozastavit</param>
public sealed record PlayRecordingCommand(string Name, double Speed, bool Loop, double Position = 0, bool Paused = false) : ICommand<ReplayStatusDto>, IAuditable
{
    string IAuditable.AuditTarget => Name;
}
