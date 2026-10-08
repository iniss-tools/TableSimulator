using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Server;

/// <summary>Chyby servera.</summary>
public static class ServerErrors
{
    /// <summary>Server nebezi ako sluzba - po ukonceni by ho nikto nespustil.</summary>
    public static readonly Error RestartUnavailable =
        Error.Conflict("server.restartUnavailable", "The server can be restarted only when it runs as a Windows service.");
}
