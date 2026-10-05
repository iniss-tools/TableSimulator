using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Application.Settings;

public sealed class GetSettingsQueryHandler(ISettingsStore settings, AppPaths paths) : IQueryHandler<GetSettingsQuery, SettingsDto>
{
    public Task<Result<SettingsDto>> Handle(GetSettingsQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(DtoMapper.ToDto(settings.Current, paths)));
}
