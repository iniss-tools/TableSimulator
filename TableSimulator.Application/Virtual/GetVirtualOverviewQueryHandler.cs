using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Application.Virtual;

public sealed class GetVirtualOverviewQueryHandler(IVirtualBoardRepository boards, XmlTableFolders folders, ISettingsStore settings, TimeProvider time)
    : IQueryHandler<GetVirtualOverviewQuery, VirtualOverviewDto>
{
    public async Task<Result<VirtualOverviewDto>> Handle(GetVirtualOverviewQuery query, CancellationToken cancellationToken)
    {
        var now = time.GetLocalNow().DateTime;
        var list = await boards.ListAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(new VirtualOverviewDto(
            list.Select(DtoMapper.ToDto).ToArray(),
            folders.Sources.Select(s => DtoMapper.ToDto(s, now)).ToArray(),
            settings.Current.VirtualFolders.Select(f => new VirtualFolderDto(f, Directory.Exists(f))).ToArray()));
    }
}
