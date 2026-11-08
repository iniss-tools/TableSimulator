using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Scenarios;

namespace TableSimulator.Contracts.Display;

/// <summary>Ustredna AK Signal (rodina <c>aksignal</c>): svetlo, rozhlas a zosuladenie banky zvukov.</summary>
/// <param name="AutoMode">automaticke svietenie - zapina ho server podla vlakov</param>
/// <param name="Manual">rucne svietenie - zapina ho obsluha servera</param>
/// <param name="Dark">snimac hlasi tmu</param>
/// <param name="LightOn">svetla svietia (rucne, alebo automatika a tma)</param>
/// <param name="LightState">stav svetla v odpovedi: bajt hex (AKSIGNAL) alebo kluce (AKSIGNALUH)</param>
/// <param name="AudioState">stav zvuku v odpovedi: bajt hex (AKSIGNAL) alebo kluce (AKSIGNALUH)</param>
/// <param name="Announcements">posledne hlasenia od najstarsieho vratane prave hrajucich</param>
public sealed record AkSignalDto(
    bool AutoMode,
    bool Manual,
    bool Dark,
    bool LightOn,
    string LightState,
    string AudioState,
    IReadOnlyList<AnnouncementDto> Announcements)
{
    /// <summary>Od kedy snimac hlasi terajsi stav; null = este nevyhodnoteny.</summary>
    public DateTimeOffset? DarkSince { get; init; }

    /// <summary>Kedy snimac podla slnka zmeni stav; null = snimac natrvalo.</summary>
    public DateTimeOffset? DarkChangesAt { get; init; }

    /// <summary>Kedy sa svetlo naposledy rozsvietilo alebo zhaslo.</summary>
    public DateTimeOffset? LightChangedAt { get; init; }

    /// <summary>Posledny prikaz servera pre svetlo.</summary>
    public DateTimeOffset? LightQueryAt { get; init; }

    /// <summary>Posledny prikaz servera pre rozhlas.</summary>
    public DateTimeOffset? AudioQueryAt { get; init; }

    /// <summary>Server sa ustredne dlhsie nepyta.</summary>
    public bool ServerSilent { get; init; }

    /// <summary>Banky zvukov z posledneho <c>sync</c>: priecinok jazyka → MD5.</summary>
    public IReadOnlyDictionary<string, string> Banks { get; init; } = new Dictionary<string, string>();

    /// <summary>Posledny prikaz <c>sync</c>.</summary>
    public DateTimeOffset? SyncedAt { get; init; }

    /// <summary>Do kedy ustredna hlasi nezosuladenu banku po <c>sync</c>.</summary>
    public DateTimeOffset? SyncUntil { get; init; }

    /// <summary>
    /// Stavy ustredne, podla ktorych prave odpoveda - z nastavenia spolu so stavmi zo scenara.
    /// </summary>
    public AkSignalConditionsDto Conditions { get; init; } = new();

    /// <summary>Co ustredni prave nastavuje beziaci scenar; null = nic.</summary>
    public ScenarioDeviceDto? Scenario { get; init; }
}
