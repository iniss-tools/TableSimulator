using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Server;

namespace TableSimulator.Application.Server;

public sealed class GetServerInfoQueryHandler(IServerHost host, AppPaths paths) : IQueryHandler<GetServerInfoQuery, ServerInfoDto>
{
    public Task<Result<ServerInfoDto>> Handle(GetServerInfoQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new ServerInfoDto(host.RunningAsService, host.Version, paths.DataFolder, host.Addresses)));
}
