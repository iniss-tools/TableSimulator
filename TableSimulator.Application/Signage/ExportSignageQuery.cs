using System.Globalization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Zaloha informacnych panelov ako ZIP (definicie a nahrate subory).</summary>
public sealed record ExportSignageQuery : IQuery<SignageExportFile>;

/// <summary>Docasny subor zalohy (po odoslani sa zmaze) a meno na stiahnutie.</summary>
public sealed record SignageExportFile(string Path, string FileName);

public sealed class ExportSignageQueryHandler(ISignageRepository repository, SignageMediaStore store, TimeProvider time)
    : IQueryHandler<ExportSignageQuery, SignageExportFile>
{
    public async Task<Result<SignageExportFile>> Handle(ExportSignageQuery query, CancellationToken cancellationToken)
    {
        var data = SignageData.From(await repository.LoadAsync(cancellationToken).ConfigureAwait(false));
        var now = time.GetLocalNow();
        // len subory, ktore spravy pouzivaju a na disku su
        var used = data.Messages.Values.Select(m => m.Definition.Media).OfType<Guid>().ToHashSet();
        var media = data.Media.Values.Where(m => used.Contains(m.Id) && File.Exists(store.PathOf(m.FileName)))
            .Select(m => new SignageBackupMedia(m.Id, m.Name, m.ContentType, m.Size, SignageBackup.EntryName(m.FileName)))
            .ToList();
        var backup = new SignageBackup(SignageBackup.FormatName, SignageBackup.CurrentVersion, now,
            [.. data.Messages.Values.OrderBy(m => m.Id)],
            [.. data.Playlists.Values.OrderBy(p => p.Id)],
            [.. data.Groups.Values.OrderBy(g => g.Id)],
            [.. data.Panels.Values.OrderBy(p => p.Id).Select(p => new SignageBackupPanel(p.Id, new SetSignagePanelRequest(p.Name, p.Note, p.GroupId)))],
            media);

        var path = Path.Combine(Path.GetTempPath(), $"tablesimulator-signage-{Guid.NewGuid():N}.zip");
        await Task.Run(() => backup.Write(path, store), cancellationToken).ConfigureAwait(false);
        return Result.Success(new SignageExportFile(path, string.Create(CultureInfo.InvariantCulture, $"informacne-panely-{now:yyyy-MM-dd}.zip")));
    }
}
