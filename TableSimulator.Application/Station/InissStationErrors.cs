using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Station;

/// <summary>Chyby nacitania instalacie INISSu.</summary>
public static class InissStationErrors
{
    /// <summary>V nastaveni nie je program INISSu.</summary>
    public static readonly Error NotConfigured = Error.Problem("iniss.notConfigured", "The INISS program is not set in the settings.");

    public static Error ProgramNotFound(string program) => Error.NotFound("iniss.programNotFound", $"The INISS program does not exist: {program}");

    public static Error DataNotFound(string folder) => Error.NotFound("iniss.dataNotFound", $"The INISS data folder does not exist: {folder}");

    public static Error Unreadable(string message) => Error.Problem("iniss.unreadable", $"The INISS installation cannot be read: {message}");

    /// <summary>Presmerovanie zmenou CommLines.xml patri len serveru INISS2.</summary>
    public static readonly Error NotIniss2 = Error.Problem("iniss.notIniss2", "The INISS program in the settings is not an INISS2 server.");

    public static Error Unwritable(string message) => Error.Problem("iniss.unwritable", $"The INISS2 lines cannot be written: {message}");
}
