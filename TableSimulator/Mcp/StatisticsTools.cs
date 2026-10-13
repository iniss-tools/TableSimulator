using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Statistics;
using TableSimulator.Contracts.Statistics;

namespace TableSimulator.Mcp;

/// <summary>Nastroje statistiky komunikacie.</summary>
[McpServerToolType]
public static class StatisticsTools
{
    [McpServerTool(Name = "get_traffic_statistics", Title = "Traffic statistics", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Communication statistics, stored once a minute: totals per line (messages by result, bytes, INISS connections), totals per board "
                 + "(messages, replies, longest gap) and line status changes in the period. The default period is the last hour, at most 31 days.")]
    [Authorize(Policy = Permissions.StatisticsView)]
    public static async Task<StatisticsResult> GetTrafficStatistics(
        IQueryHandler<GetTrafficStatisticsQuery, TrafficStatisticsDto> handler,
        TimeProvider time,
        [Description("Start of the period; default one hour before the end.")] DateTimeOffset? from = null,
        [Description("End of the period; default now.")] DateTimeOffset? to = null,
        [Description("Only this line.")] int? line = null,
        [Description("Include the counters of each line per interval (the course over time).")] bool series = false,
        CancellationToken cancellationToken = default)
    {
        var end = to ?? time.GetUtcNow();
        var start = from ?? end - TimeSpan.FromHours(1);
        var statistics = (await handler.Handle(new GetTrafficStatisticsQuery(start, end, line), cancellationToken)).ValueOrThrow();
        return McpMapper.ToStatistics(statistics, series);
    }
}
