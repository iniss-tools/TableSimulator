using Microsoft.Extensions.Logging;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Station;
using ToolsCore.Iniss2.Tables;

namespace TableSimulator.Application.Station;

/// <summary>
/// Prepise adresy liniek v CommLines.xml servera. Server subor cita len pri starte, zmena plati az po jeho restarte. INISS 3.39
/// presmeruje GVDEditor cez subor .INI.
/// </summary>
public sealed partial class RedirectInissLinesCommandHandler(ISettingsStore settings, ILogger<RedirectInissLinesCommandHandler> logger)
    : ICommandHandler<RedirectInissLinesCommand, InissStationDto>
{
    public Task<Result<InissStationDto>> Handle(RedirectInissLinesCommand command, CancellationToken cancellationToken)
    {
        var station = InissStations.Read(settings.Current);
        if (station.IsFailure)
            return Task.FromResult(station);

        if (InissStations.Iniss2Of(settings.Current) is not { } installation)
            return Task.FromResult(Result.Failure<InissStationDto>(InissStationErrors.NotIniss2));

        var addresses = command.Request.Redirect ? Iniss2Stations.RedirectAddresses(station.Value) : [];
        try
        {
            CommLinesXml.Write(installation.DataDir, addresses);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LogFailed(e, installation.DataDir);
            return Task.FromResult(Result.Failure<InissStationDto>(InissStationErrors.Unwritable(e.Message)));
        }

        LogRedirected(installation.DataDir, addresses.Count);
        return Task.FromResult(InissStations.Read(settings.Current));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Linky servera INISS2 v {Folder} sa nepodarilo presmerovat")]
    private partial void LogFailed(Exception exception, string folder);

    [LoggerMessage(Level = LogLevel.Information, Message = "Linky servera INISS2 v {Folder}: presmerovanych {Count}")]
    private partial void LogRedirected(string folder, int count);
}
