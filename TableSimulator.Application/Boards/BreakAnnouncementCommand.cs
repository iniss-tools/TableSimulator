using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Boards;

/// <summary>
/// Prerusi hlasenie rozhlasu vzdialeneho INISSu: riadiaci INISS ho ukonci s vysledkom <c>remote break</c>.
/// </summary>
/// <param name="Id">rozhlas vzdialeneho INISSu <c>linka-adresa</c></param>
/// <param name="Queue">fronta ("" = jedina fronta); null = vsetky fronty</param>
public sealed record BreakAnnouncementCommand(string Id, string? Queue) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Queue is null ? Id : $"{Id}/{Queue}";
}

public sealed class BreakAnnouncementCommandHandler(Core.Simulation.Simulator simulator) : ICommandHandler<BreakAnnouncementCommand>
{
    public Task<Result> Handle(BreakAnnouncementCommand command, CancellationToken cancellationToken)
    {
        var result = simulator.BreakAnnouncement(command.Id, command.Queue);
        return Task.FromResult(result switch
        {
            Core.Simulation.BreakResult.Broken => Result.Success(),
            Core.Simulation.BreakResult.NotFound => Result.Failure(BoardErrors.NotFound(command.Id)),
            Core.Simulation.BreakResult.NotRemoteAudio => Result.Failure(BoardErrors.NotRemoteAudio(command.Id)),
            _ => Result.Failure(BoardErrors.NothingPlaying(command.Id))
        });
    }
}
