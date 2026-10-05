using TableSimulator.Contracts.Live;

namespace TableSimulator.Application.Live;

/// <summary>Posiela zmeny prehliadacom (WebSocket) - implementuje server.</summary>
public interface ILiveNotifier
{
    /// <summary>Posle spravu vsetkym pripojenym prehliadacom.</summary>
    void Publish(LiveMessage message);
}
