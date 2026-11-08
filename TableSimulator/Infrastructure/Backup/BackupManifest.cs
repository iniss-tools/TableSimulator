using TableSimulator.Contracts.Backup;
using TableSimulator.Infrastructure.Hosting;

namespace TableSimulator.Infrastructure.Backup;

/// <summary>Popis zalohy v ZIP (<c>backup.json</c>).</summary>
/// <param name="Format">vzdy <see cref="ServerBackup.FormatName" /></param>
/// <param name="Version">verzia formatu</param>
/// <param name="Created">kedy vznikla</param>
/// <param name="ServerVersion">verzia servera</param>
/// <param name="Migration">posledna migracia databazy</param>
/// <param name="Designs">pocet vlastnych vzhladov</param>
/// <param name="Media">pocet suborov informacnych panelov</param>
/// <param name="Recordings">pocet zaznamov komunikacie; null = bez zaznamov</param>
/// <param name="Lines">pocet liniek v databaze; pri obnove sa zisti znova</param>
/// <param name="Boards">pocet tabul v databaze; pri obnove sa zisti znova</param>
/// <param name="Machine">pocitac, na ktorom zaloha vznikla</param>
/// <param name="DataFolder">priecinok nastaveni zalohovaneho servera</param>
/// <param name="Service">zalohovany server bezal ako sluzba Windows</param>
public sealed record BackupManifest(string Format, int Version, DateTimeOffset Created, string ServerVersion, string? Migration, int Designs, int Media,
    int? Recordings, int? Lines = null, int? Boards = null, string? Machine = null, string? DataFolder = null, bool? Service = null)
{
    /// <summary>Popis zalohy pre server s priecinkom nastaveni <paramref name="dataFolder" />.</summary>
    public BackupInfoDto ToDto(string dataFolder) => new(Created, ServerVersion, Designs, Media, Recordings, Lines, Boards, Machine, DataFolder, Service,
        string.Equals(Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) && DataFolder is not null && RunningServer.SamePath(DataFolder, dataFolder));
}
