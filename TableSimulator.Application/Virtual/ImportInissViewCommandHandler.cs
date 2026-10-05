using System.Xml;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Application.Virtual;

public sealed class ImportInissViewCommandHandler(IVirtualBoardRepository boards, ILiveNotifier live)
    : ICommandHandler<ImportInissViewCommand, InissViewImportedDto>
{
    public async Task<Result<InissViewImportedDto>> Handle(ImportInissViewCommand command, CancellationToken cancellationToken)
    {
        InissViewConfig config;
        try
        {
            config = InissViewConfig.Parse(command.Content);
        }
        catch (XmlException e)
        {
            return Result.Failure<InissViewImportedDto>(VirtualErrors.InvalidConfig(e.Message));
        }

        // cisla mimo rozsahu simulatora sa vynechaju rovnako ako duplicity
        var tables = config.Tables.Where(t => t.Number is >= 1 and <= VirtualErrors.MaxId).ToArray();
        if (tables.Length == 0)
            return Result.Failure<InissViewImportedDto>(VirtualErrors.NoTables());

        var counts = await boards.ImportAsync(tables.Select(RequestMapper.ToVirtualBoard).ToArray(), command.Replace, cancellationToken)
            .ConfigureAwait(false);
        live.Publish(new VirtualMessage([], true));
        return Result.Success(new InissViewImportedDto(counts.Added, counts.Replaced, counts.Skipped + config.Tables.Count - tables.Length,
            config.Duplicates, config.XmlFolders));
    }
}
