using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Program panelu - co ma hrat; zaroven zapise, ze sa panel ozval.</summary>
/// <param name="Panel">cislo panelu</param>
/// <param name="Address">adresa zariadenia</param>
public sealed record GetSignageProgramQuery(int Panel, string Address) : IQuery<SignageProgramDto>;

public sealed class GetSignageProgramQueryHandler(ISignageRepository repository, SignagePanelTracker tracker, TimeProvider time)
    : IQueryHandler<GetSignageProgramQuery, SignageProgramDto>
{
    /// <summary>Bez skupiny sa panel pyta znova o tolko (alebo po zmene na serveri).</summary>
    private static readonly TimeSpan Idle = TimeSpan.FromHours(1);

    public async Task<Result<SignageProgramDto>> Handle(GetSignageProgramQuery query, CancellationToken cancellationToken)
    {
        var data = SignageData.From(await repository.LoadAsync(cancellationToken).ConfigureAwait(false));
        if (!data.Panels.TryGetValue(query.Panel, out var panel))
            return Result.Failure<SignageProgramDto>(SignageErrors.NotFound(SignageEntity.Panel, query.Panel));

        var now = time.GetUtcNow();
        tracker.Touch(panel.Id, query.Address, now);
        if (panel.GroupId is not { } groupId || !data.Groups.TryGetValue(groupId, out var group))
            return Result.Success(new SignageProgramDto(now, panel.Name, null, null, now, [], now + Idle, SignageOffline.NoGroup));

        var local = time.GetLocalNow().DateTime;
        var plan = data.PlanFor(group, local);
        return Result.Success(new SignageProgramDto(now, panel.Name, group, plan.Playlist, Offset(plan.Anchor), plan.Items, Offset(plan.NextChange), plan.Offline));
    }

    /// <summary>Miestny cas servera s posunom jeho casoveho pasma.</summary>
    private DateTimeOffset Offset(DateTime local) => new(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), time.LocalTimeZone.GetUtcOffset(local));
}
