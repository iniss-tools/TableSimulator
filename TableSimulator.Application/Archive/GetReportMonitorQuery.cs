using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Archive;

namespace TableSimulator.Application.Archive;

/// <summary>Monitor hlaseni - najnovsich <paramref name="Limit" /> zaznamov sledovaneho archivu.</summary>
public sealed record GetReportMonitorQuery(int Limit) : IQuery<ReportMonitorDto>;

public sealed class GetReportMonitorQueryValidator : AbstractValidator<GetReportMonitorQuery>
{
    public GetReportMonitorQueryValidator()
    {
        RuleFor(q => q.Limit).InclusiveBetween(1, ReportMonitor.MaxEntries).WithErrorCode(ArchiveErrors.Page);
    }
}

public sealed class GetReportMonitorQueryHandler(ReportMonitor monitor, ISettingsStore settings) : IQueryHandler<GetReportMonitorQuery, ReportMonitorDto>
{
    public Task<Result<ReportMonitorDto>> Handle(GetReportMonitorQuery query, CancellationToken cancellationToken)
    {
        // prve otvorenie hned po starte servera nemusi cakat na prvy prechod sluzby
        try
        {
            monitor.Poll(settings.Current.VirtualFolders);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Failure<ReportMonitorDto>(ArchiveErrors.Unreadable(e.Message)));
        }

        return Task.FromResult(Result.Success(monitor.Snapshot(query.Limit)));
    }
}
