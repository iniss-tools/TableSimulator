namespace TableSimulator.Application.Server;

/// <summary>Proces servera: ako bezi a restart (len ako sluzba Windows - Windows ho po ukonceni spusti znova).</summary>
public interface IServerHost
{
    /// <summary>Server bezi ako sluzba Windows.</summary>
    bool RunningAsService { get; }

    /// <summary>Verzia servera.</summary>
    string Version { get; }

    /// <summary>Adresy tohto pocitaca v sieti: IPv4 aktivnych rozhrani a meno pocitaca.</summary>
    IReadOnlyList<string> Addresses { get; }

    /// <summary>Ukonci server tak, aby ho Windows spustil znova (po odoslani odpovede).</summary>
    void Restart();
}
