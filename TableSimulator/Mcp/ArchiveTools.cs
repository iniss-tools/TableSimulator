using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Archive;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Archive;

namespace TableSimulator.Mcp;

/// <summary>Nastroje dennych archivov INISSu (priecinok logov z nastavenia simulatora).</summary>
[McpServerToolType]
public static class ArchiveTools
{
    /// <summary>Najviac zaznamov archivu v jednom vysledku.</summary>
    public const int MaxLimit = 200;

    private const string DateDescription = "Day of the archive (yyyy-MM-dd); without it the newest day is used.";
    private const string TrainDescription = "Only entries of this train (part of the train number).";
    private const string TextDescription = "Only entries containing this text (diacritics ignored).";
    private const string NewestDescription = "Newest entries first.";
    private const string OffsetDescription = "How many matching entries to skip (paging).";
    private const string LimitDescription = "Maximum number of entries (1-200).";

    [McpServerTool(Name = "get_table_archive", Title = "Board archive", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Daily archive in which INISS logs what it sent to the boards: time, station, train, text and the values of the board columns. "
                 + "Read from the INISS log folder set in the simulator.")]
    [Authorize(Policy = Permissions.ArchiveView)]
    public static async Task<TableArchiveResult> GetTableArchive(
        IQueryHandler<GetArchiveDaysQuery, ArchiveDaysDto> days,
        IQueryHandler<GetTableArchiveQuery, TableArchivePageDto> handler,
        [Description(DateDescription)] DateOnly? date = null,
        [Description(TrainDescription)] string? train = null,
        [Description(TextDescription)] string? text = null,
        [Description(NewestDescription)] bool newest = true,
        [Description(OffsetDescription)] int offset = 0,
        [Description(LimitDescription)] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        McpResults.CheckLimit(limit, MaxLimit, "archive.limit");
        var available = McpMapper.RecentDays((await days.Handle(new GetArchiveDaysQuery(), cancellationToken)).ValueOrThrow().Tables);
        if ((date ?? available.Cast<DateOnly?>().FirstOrDefault()) is not { } day)
            return new TableArchiveResult(null, available, 0, 0, []);
        var page = (await handler.Handle(new GetTableArchiveQuery(day, train, text, newest, offset, limit), cancellationToken)).ValueOrThrow();
        return new TableArchiveResult(page.Date, available, page.Total, page.Skipped, page.Entries);
    }

    [McpServerTool(Name = "get_report_archive", Title = "Announcement archive", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Daily archive of the announcements INISS made: time, kind, train, tracks, delays, text and result. "
                 + "Read from the INISS log folder set in the simulator.")]
    [Authorize(Policy = Permissions.ArchiveView)]
    public static async Task<ReportArchiveResult> GetReportArchive(
        IQueryHandler<GetArchiveDaysQuery, ArchiveDaysDto> days,
        IQueryHandler<GetReportArchiveQuery, ReportArchivePageDto> handler,
        [Description(DateDescription)] DateOnly? date = null,
        [Description(TrainDescription)] string? train = null,
        [Description(TextDescription)] string? text = null,
        [Description(NewestDescription)] bool newest = true,
        [Description(OffsetDescription)] int offset = 0,
        [Description(LimitDescription)] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        McpResults.CheckLimit(limit, MaxLimit, "archive.limit");
        var available = McpMapper.RecentDays((await days.Handle(new GetArchiveDaysQuery(), cancellationToken)).ValueOrThrow().Reports);
        if ((date ?? available.Cast<DateOnly?>().FirstOrDefault()) is not { } day)
            return new ReportArchiveResult(null, available, 0, 0, []);
        var page = (await handler.Handle(new GetReportArchiveQuery(day, train, text, newest, offset, limit), cancellationToken)).ValueOrThrow();
        return new ReportArchiveResult(page.Date, available, page.Total, page.Skipped, page.Entries);
    }

    [McpServerTool(Name = "get_recent_reports", Title = "Recent announcements", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("The newest announcements from the announcement archive INISS is writing now, newest first.")]
    [Authorize(Policy = Permissions.ArchiveView)]
    public static async Task<ReportMonitorDto> GetRecentReports(
        IQueryHandler<GetReportMonitorQuery, ReportMonitorDto> handler,
        [Description(LimitDescription)] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        McpResults.CheckLimit(limit, MaxLimit, "archive.limit");
        return (await handler.Handle(new GetReportMonitorQuery(limit), cancellationToken)).ValueOrThrow();
    }
}
