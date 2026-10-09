using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Panel v zalohe.</summary>
public sealed record SignageBackupPanel(int Id, SetSignagePanelRequest Definition);

/// <summary>Nahraty subor v zalohe; obsah je v ZIP pod <paramref name="File" />.</summary>
public sealed record SignageBackupMedia(Guid Id, string Name, string ContentType, long Size, string File);

/// <summary>
/// Zaloha informacnych panelov - <c>signage.json</c> v ZIP spolu s nahratymi subormi v <c>media/</c>. Cisla
/// zaznamov su tie zo simulatora (obnova ich zachova, pridanie ich precisluje).
/// </summary>
public sealed partial record SignageBackup(
    string Format,
    int Version,
    DateTimeOffset Exported,
    IReadOnlyList<SignageMessageDto> Messages,
    IReadOnlyList<SignagePlaylistDto> Playlists,
    IReadOnlyList<SignageGroupDto> Groups,
    IReadOnlyList<SignageBackupPanel> Panels,
    IReadOnlyList<SignageBackupMedia> Media)
{
    /// <summary>Oznacenie formatu.</summary>
    public const string FormatName = "TableSimulator.Signage";

    /// <summary>Verzia formatu.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Subor s definiciami v ZIP.</summary>
    public const string DataEntry = "signage.json";

    /// <summary>Najvacsi subor s definiciami.</summary>
    private const long MaxDataBytes = 20 * 1024 * 1024;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Meno suboru v ZIP podla mena v priecinku media.</summary>
    public static string EntryName(string fileName) => "media/" + fileName;

    /// <summary>Zapise zalohu do ZIP <paramref name="path" />; subory berie z <paramref name="store" />.</summary>
    public void Write(string path, SignageMediaStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        var data = zip.CreateEntry(DataEntry, CompressionLevel.Optimal);
        using (var stream = data.Open())
            JsonSerializer.Serialize(stream, this, Json);
        // obrazky a videa su uz skomprimovane
        foreach (var media in Media)
            zip.CreateEntryFromFile(store.PathOf(media.File[(media.File.LastIndexOf('/') + 1)..]), media.File, CompressionLevel.NoCompression);
    }

    /// <summary>Precita zalohu zo ZIP; null = nie je to zaloha informacnych panelov.</summary>
    /// <exception cref="InvalidDataException">poskodeny ZIP</exception>
    public static SignageBackup? Read(ZipArchive zip)
    {
        ArgumentNullException.ThrowIfNull(zip);
        if (zip.GetEntry(DataEntry) is not { } entry || entry.Length > MaxDataBytes)
            return null;
        try
        {
            using var stream = entry.Open();
            var backup = JsonSerializer.Deserialize<SignageBackup>(stream, Json);
            return backup is { Format: FormatName, Version: > 0 and <= CurrentVersion } ? backup : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Meno suboru v ZIP je <c>media/&lt;32 hex&gt;.&lt;pripona&gt;</c> - nic ine sa nerozbali.</summary>
    public static bool IsMediaEntry(string name) => MediaEntry().IsMatch(name);

    [GeneratedRegex(@"^media/[0-9a-f]{32}\.[a-z0-9]{2,5}$", RegexOptions.CultureInvariant)]
    private static partial Regex MediaEntry();
}
