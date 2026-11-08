using System.Globalization;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Application.Station;
using TableSimulator.Contracts.AkSignal;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;
using ToolsCore.Iniss2.Devices;

namespace TableSimulator.Application.Devices;

/// <summary>Historia ustredni AK Signal za obdobie [<paramref name="From" />, <paramref name="To" />); ustredna null = vsetky.</summary>
public sealed record GetDeviceHistoryQuery(DateTimeOffset From, DateTimeOffset To, string? Board) : IQuery<DeviceHistoryDto>;

public sealed class GetDeviceHistoryQueryValidator : AbstractValidator<GetDeviceHistoryQuery>
{
    public const string Range = "history.range";
    public const string Board = "history.board";

    public GetDeviceHistoryQueryValidator()
    {
        RuleFor(q => q.To).GreaterThan(q => q.From).WithErrorCode(Range).WithMessage("The end must be after the start.");
        RuleFor(q => q).Must(q => q.To - q.From <= TimeSpan.FromDays(DeviceHistoryRetention.MaxSpanDays)).WithErrorCode(Range)
            .WithMessage($"The period must not be longer than {DeviceHistoryRetention.MaxSpanDays} days.");
        RuleFor(q => q.Board).Must(b => GetDeviceHistoryQueryHandler.ParseBoard(b) is not null).When(q => q.Board is not null)
            .WithErrorCode(Board).WithMessage("The board must be line-address.");
    }
}

public sealed class GetDeviceHistoryQueryHandler(IDeviceHistoryRepository history, Simulator simulator, ISettingsStore settings)
    : IQueryHandler<GetDeviceHistoryQuery, DeviceHistoryDto>
{
    /// <summary>Linka a adresa z identifikatora <c>linka-adresa</c>; null = neplatny.</summary>
    public static (int Line, int Address)? ParseBoard(string? id)
    {
        var parts = id?.Split('-');
        return parts is [var line, var address]
               && int.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out var l)
               && int.TryParse(address, NumberStyles.None, CultureInfo.InvariantCulture, out var a)
            ? (l, a)
            : null;
    }

    public async Task<Result<DeviceHistoryDto>> Handle(GetDeviceHistoryQuery query, CancellationToken cancellationToken)
    {
        var board = ParseBoard(query.Board);
        var data = await history.QueryAsync(query.From.ToUnixTimeMilliseconds(), query.To.ToUnixTimeMilliseconds(), board?.Line, board?.Address,
            cancellationToken).ConfigureAwait(false);

        var lights = data.Lights.OrderBy(r => r.TimeMs).ThenBy(r => r.Id).Select(ToDto).ToList();
        var announcements = data.Announcements.OrderBy(r => r.StartMs).ThenBy(r => r.Id).Select(ToDto).ToList();
        var server = ServerLog(query, board is { } b ? SimBoard.MakeId(b.Line, b.Address) : null);
        return Result.Success(new DeviceHistoryDto(query.From, query.To, lights, announcements, server));
    }

    private static LightStateDto ToDto(LightStateRecord r)
    {
        var flags = (LightFlags)r.Flags;
        return new LightStateDto(SimBoard.MakeId(r.Line, r.Address), DateTimeOffset.FromUnixTimeMilliseconds(r.TimeMs), flags.HasFlag(LightFlags.Auto),
            flags.HasFlag(LightFlags.Manual), flags.HasFlag(LightFlags.Dark), flags.HasFlag(LightFlags.On), flags.HasFlag(LightFlags.Fault),
            flags.HasFlag(LightFlags.Silent), flags.HasFlag(LightFlags.PoweredOff));
    }

    private static AnnouncementHistoryDto ToDto(AnnouncementRecord r) =>
        new(SimBoard.MakeId(r.Line, r.Address), r.Channel, r.Sounds.Split('*', StringSplitOptions.RemoveEmptyEntries),
            DateTimeOffset.FromUnixTimeMilliseconds(r.StartMs), DateTimeOffset.FromUnixTimeMilliseconds(r.EndMs), r.Result switch
            {
                "stopped" => Contracts.Display.AnnouncementResult.Stopped,
                "cancelled" => Contracts.Display.AnnouncementResult.Cancelled,
                _ => Contracts.Display.AnnouncementResult.Completed
            })
        {
            Value = r.Value,
            Title = r.Title,
            Queue = r.Queue,
            Circuit = r.Circuit
        };

    /// <summary>
    /// Log svetiel servera INISS2 z nastavenia za dni obdobia; svetlo sa ustredni priradi podla nazvu (log ma nazov stanice,
    /// nie cislo).
    /// </summary>
    private List<ServerLightEventDto> ServerLog(GetDeviceHistoryQuery query, string? board)
    {
        if (InissStations.Iniss2Of(settings.Current) is not { } installation)
            return [];

        var owners = simulator.Read(s => s.Boards
            .Where(b => b.Display is AkSignalDisplay)
            .SelectMany(b => (b.Profile.AkSignal ?? AkSignalProfile.Default).Lights.Select(l => (l.Name, b.Id)))
            .DistinctBy(l => l.Name, StringComparer.Ordinal)
            .ToDictionary(l => l.Name, l => l.Id, StringComparer.Ordinal));

        var events = new List<ServerLightEventDto>();
        var from = DateOnly.FromDateTime(query.From.LocalDateTime);
        var to = DateOnly.FromDateTime(query.To.LocalDateTime);
        for (var day = from; day <= to && events.Count < DeviceHistoryRetention.MaxServerEvents; day = day.AddDays(1))
        {
            List<LightLogEntry> entries;
            try
            {
                entries = LightLogFile.Read(LightLogFile.PathFor(installation.LogDir, day));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // log sa da precitat aj pocas zapisu; nedostupny disk = bez logu servera
                continue;
            }

            foreach (var entry in entries.Where(e => e.At >= query.From && e.At < query.To))
            {
                var owner = owners.GetValueOrDefault(entry.Name);
                if (board is not null && owner != board)
                    continue;
                events.Add(new ServerLightEventDto(entry.At, entry.Station, entry.Name, entry.Event switch
                {
                    LightLogFile.ChangeForTrain => ServerLightEvent.Train,
                    LightLogFile.SetMode => ServerLightEvent.Mode,
                    LightLogFile.ChangeState => ServerLightEvent.State,
                    _ => ServerLightEvent.Other
                })
                {
                    BoardId = owner,
                    Manual = entry.Manual,
                    Mode = entry.Mode,
                    TrainNeedsLight = entry.TrainNeedsLight,
                    State = entry.State,
                    TurnOn = entry.TurnOn,
                    TurnOff = entry.TurnOff,
                    Train = entry.Train
                });
            }
        }

        return events.OrderBy(e => e.At).Take(DeviceHistoryRetention.MaxServerEvents).ToList();
    }
}
