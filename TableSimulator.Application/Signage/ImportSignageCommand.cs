using System.Globalization;
using System.IO.Compression;
using System.Text.Json.Serialization;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>
/// Obnovi zalohu informacnych panelov: <paramref name="Replace" /> = vsetko nahradi zalohou s povodnymi cislami
/// (monitory s <c>#/panel/N</c> idu dalej), inak obsah zalohy prida s novymi cislami. Obsah do auditu nejde.
/// </summary>
public sealed record ImportSignageCommand(bool Replace, [property: JsonIgnore] Stream Content) : ICommand<SignageImportedDto>, IAuditable
{
    string IAuditable.AuditTarget => Replace ? "replace" : "add";
}

public sealed class ImportSignageCommandHandler(ISignageRepository repository, SignageMediaStore store, ILiveNotifier live, TimeProvider time)
    : ICommandHandler<ImportSignageCommand, SignageImportedDto>
{
    /// <summary>Najvacsia zaloha.</summary>
    public const long MaxBytes = 4L * 1024 * 1024 * 1024;

    public async Task<Result<SignageImportedDto>> Handle(ImportSignageCommand command, CancellationToken cancellationToken)
    {
        // ZIP sa cita z disku - zaloha s videami moze byt velka
        var temp = Path.Combine(Path.GetTempPath(), $"tablesimulator-import-{Guid.NewGuid():N}.zip");
        try
        {
            if (!await CopyAsync(command.Content, temp, cancellationToken).ConfigureAwait(false))
                return Result.Failure<SignageImportedDto>(SignageErrors.ImportTooLarge());
            using var zip = OpenZip(temp);
            if (zip is null || SignageBackup.Read(zip) is not { } backup)
                return Result.Failure<SignageImportedDto>(SignageErrors.ImportFormat());
            if (Problem(backup, zip) is { } problem)
                return Result.Failure<SignageImportedDto>(SignageErrors.ImportInvalid(problem));

            var existing = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            var records = Records(backup, existing, command.Replace);
            var written = Extract(zip, backup, records.Media);
            try
            {
                await repository.ImportAsync(records, command.Replace, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                store.Delete(written);
                throw;
            }

            // stare subory, ktore zaloha nema
            if (command.Replace)
                store.Delete(existing.Media.Where(m => records.Media.All(r => r.Id != m.Id)));

            live.Publish(new SignageChangedMessage());
            return Result.Success(new SignageImportedDto(records.Messages.Count, records.Playlists.Count, records.Groups.Count, records.Panels.Count,
                records.Media.Count, command.Replace));
        }
        finally
        {
            File.Delete(temp);
        }
    }

    private static async Task<bool> CopyAsync(Stream content, string path, CancellationToken cancellationToken)
    {
        var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await using (file.ConfigureAwait(false))
        {
            var buffer = new byte[81920];
            long size = 0;
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                size += read;
                if (size > MaxBytes)
                    return false;
                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }

        return true;
    }

    private static ZipArchive? OpenZip(string path)
    {
        try
        {
            return ZipFile.OpenRead(path);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>Co je so zalohou zle (definicie podla pravidiel API, odkazy medzi nimi, subory v ZIP); null = v poriadku.</summary>
    private static string? Problem(SignageBackup backup, ZipArchive zip)
    {
        var problems = new List<string>();
        void Check<T>(AbstractValidator<T> rules, T definition, string what)
        {
            var result = rules.Validate(definition);
            if (!result.IsValid)
                problems.Add($"{what}: {result.Errors[0].ErrorMessage}");
        }

        foreach (var m in backup.Messages)
            Check(new SignageMessageRules(), m.Definition, $"message {m.Id}");
        foreach (var p in backup.Playlists)
            Check(new SignagePlaylistRules(), p.Definition, $"playlist {p.Id}");
        foreach (var g in backup.Groups)
            Check(new SignageGroupRules(), g.Definition, $"group {g.Id}");
        foreach (var p in backup.Panels)
            Check(new SignagePanelRules(), p.Definition, $"panel {p.Id}");

        var messages = backup.Messages.Select(m => m.Id).ToHashSet();
        var playlists = backup.Playlists.Select(p => p.Id).ToHashSet();
        var groups = backup.Groups.Select(g => g.Id).ToHashSet();
        var media = backup.Media.Select(m => m.Id).ToHashSet();
        var panels = backup.Panels.Select(p => p.Id).ToHashSet();
        if (messages.Count != backup.Messages.Count || playlists.Count != backup.Playlists.Count || groups.Count != backup.Groups.Count
            || panels.Count != backup.Panels.Count || messages.Concat(playlists).Concat(groups).Concat(panels).Any(id => id <= 0))
            problems.Add("duplicate or invalid numbers");
        problems.AddRange(backup.Playlists.Where(p => p.Definition.Items.Any(i => !messages.Contains(i.Message))).Select(p => $"playlist {p.Id}: unknown message"));
        problems.AddRange(backup.Groups
            .Where(g => g.Definition.Playlists.Any(p => !playlists.Contains(p))
                        || g.Definition.DefaultPlaylist is { } d && !playlists.Contains(d)
                        || g.Definition.DefaultMessage is { } m && !messages.Contains(m))
            .Select(g => $"group {g.Id}: unknown playlist or message"));
        problems.AddRange(backup.Panels.Where(p => p.Definition.Group is { } g && !groups.Contains(g)).Select(p => $"panel {p.Id}: unknown group"));
        problems.AddRange(backup.Messages.Where(m => m.Definition.Media is { } id && !media.Contains(id)).Select(m => $"message {m.Id}: missing file"));
        problems.AddRange(backup.Media
            .Where(m => !SignageBackup.IsMediaEntry(m.File) || !SignageMediaStore.Types.ContainsKey(Path.GetExtension(m.File))
                        || zip.GetEntry(m.File) is not { } entry || entry.Length > SignageMediaStore.MaxBytes)
            .Select(m => $"file {m.File}: missing or not allowed"));
        return problems.Count == 0 ? null : string.Join("; ", problems.Take(5));
    }

    /// <summary>
    /// Zaznamy na zapis: pri nahradeni s cislami zalohy, pri pridani za najvyssie existujuce cisla (odkazy medzi nimi sa
    /// precisluju). Subory s rovnakym cislom, ake uz v simulatore su, sa pri pridani nezapisuju.
    /// </summary>
    private SignageRecords Records(SignageBackup backup, SignageRecords existing, bool replace)
    {
        var messageIds = Numbers(backup.Messages.Select(m => m.Id), existing.Messages.Select(m => m.Id), replace);
        var playlistIds = Numbers(backup.Playlists.Select(p => p.Id), existing.Playlists.Select(p => p.Id), replace);
        var groupIds = Numbers(backup.Groups.Select(g => g.Id), existing.Groups.Select(g => g.Id), replace);
        var panelIds = Numbers(backup.Panels.Select(p => p.Id), existing.Panels.Select(p => p.Id), replace);

        var messages = backup.Messages.Select(m => new SignageMessageRecord(messageIds[m.Id], m.Definition.Name, SignageJson.Serialize(m.Definition))).ToList();
        var playlists = backup.Playlists.Select(p =>
        {
            var definition = p.Definition with { Items = [.. p.Definition.Items.Select(i => i with { Message = messageIds[i.Message] })] };
            return new SignagePlaylistRecord(playlistIds[p.Id], definition.Name, SignageJson.Serialize(definition));
        }).ToList();
        var groups = backup.Groups.Select(g =>
        {
            var definition = g.Definition with
            {
                Playlists = [.. g.Definition.Playlists.Select(p => playlistIds[p])],
                DefaultPlaylist = g.Definition.DefaultPlaylist is { } d ? playlistIds[d] : null,
                DefaultMessage = g.Definition.DefaultMessage is { } m ? messageIds[m] : null
            };
            return new SignageGroupRecord(groupIds[g.Id], definition.Name, SignageJson.Serialize(definition));
        }).ToList();
        var panels = backup.Panels
            .Select(p => new SignagePanelRecord(panelIds[p.Id], p.Definition.Name, p.Definition.Note, p.Definition.Group is { } g ? groupIds[g] : null))
            .ToList();

        var now = time.GetUtcNow();
        HashSet<Guid> known = replace ? [] : [.. existing.Media.Select(m => m.Id)];
        var media = backup.Media.Where(m => !known.Contains(m.Id))
            .Select(m => new SignageMediaRecord(m.Id, m.Name, m.ContentType, m.Size, FileName(m), now))
            .ToList();
        return new SignageRecords(messages, playlists, groups, panels, media);
    }

    /// <summary>Cisla zo zalohy na cisla v simulatore.</summary>
    private static Dictionary<int, int> Numbers(IEnumerable<int> backup, IEnumerable<int> existing, bool replace)
    {
        var next = replace ? 0 : existing.DefaultIfEmpty(0).Max();
        return backup.Order().ToDictionary(id => id, id => replace ? id : ++next);
    }

    /// <summary>Meno suboru v priecinku media: cislo suboru a pripona (meno v ZIP sa nepouzije).</summary>
    private static string FileName(SignageBackupMedia media) =>
        media.Id.ToString("N") + Path.GetExtension(media.File).ToLower(CultureInfo.InvariantCulture);

    /// <summary>Rozbali nove subory; vrati tie, ktore zapisal (pri chybe sa zmazu).</summary>
    private List<SignageMediaRecord> Extract(ZipArchive zip, SignageBackup backup, IReadOnlyList<SignageMediaRecord> media)
    {
        var written = new List<SignageMediaRecord>();
        try
        {
            foreach (var record in media)
            {
                // subor s tym istym cislom je ten isty (obsah sa pod cislom nemeni)
                if (File.Exists(store.PathOf(record.FileName)))
                    continue;
                var entry = zip.GetEntry(backup.Media.First(m => m.Id == record.Id).File)!;
                Directory.CreateDirectory(Path.GetDirectoryName(store.PathOf(record.FileName))!);
                entry.ExtractToFile(store.PathOf(record.FileName));
                written.Add(record);
            }
        }
        catch
        {
            store.Delete(written);
            throw;
        }

        return written;
    }
}
