using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Spravy, playlisty, skupiny a panely s tym, co prave hraju.</summary>
public sealed record GetSignageOverviewQuery : IQuery<SignageOverviewDto>;

public sealed class GetSignageOverviewQueryHandler(ISignageRepository repository, SignagePanelTracker tracker, TimeProvider time)
    : IQueryHandler<GetSignageOverviewQuery, SignageOverviewDto>
{
    public async Task<Result<SignageOverviewDto>> Handle(GetSignageOverviewQuery query, CancellationToken cancellationToken)
    {
        var data = SignageData.From(await repository.LoadAsync(cancellationToken).ConfigureAwait(false));
        var now = time.GetUtcNow();
        var local = time.GetLocalNow().DateTime;
        var plans = data.Groups.Values.ToDictionary(g => g.Id, g => data.PlanFor(g, local));
        var panels = data.Panels.Values.OrderBy(p => p.Id).Select(p => new SignagePanelDto(
            p.Id,
            new SetSignagePanelRequest(p.Name, p.Note, p.GroupId),
            tracker.Status(p.Id, now),
            p.GroupId is { } g && plans.TryGetValue(g, out var plan) ? plan.PlayingAt(local)?.Id : null)).ToList();
        return Result.Success(new SignageOverviewDto(
            [.. data.Messages.Values.OrderBy(m => m.Id)],
            [.. data.Playlists.Values.OrderBy(p => p.Id)],
            [.. data.Groups.Values.OrderBy(g => g.Id)],
            panels));
    }
}
