namespace TableSimulator.Application.Auth;

/// <summary>
/// Opravnenia podla cinnosti - nazvy su zaroven nazvy politik autorizacie, kluce v rolach (databaza) a pre webove
/// rozhranie. Kluc sa nesmie menit; nezname kluce v ulozenych rolach sa ignoruju.
/// </summary>
public static class Permissions
{
    /// <summary>Prehlad: linky, tabule, stav, pisma, vzhlady, zaznamy, scenare, virtualne tabule a panely (len citanie).</summary>
    public const string SimulatorView = "simulator.view";

    /// <summary>Vidiet zaznam komunikacie.</summary>
    public const string LogView = "log.view";

    /// <summary>Archiv tabul a hlaseni, monitor hlaseni.</summary>
    public const string ArchiveView = "archive.view";

    /// <summary>Statistiky komunikacie.</summary>
    public const string StatisticsView = "statistics.view";

    /// <summary>Prezerat audit - kto co urobil.</summary>
    public const string AuditView = "audit.view";

    /// <summary>Linky a tabule: pridat, upravit, zmazat, import stanice, konfiguracie a projektov Monitora.</summary>
    public const string LinesManage = "lines.manage";

    /// <summary>Ovladanie tabul: poruchy, vymazanie obsahu tabule a zaznamu komunikacie.</summary>
    public const string BoardsControl = "boards.control";

    /// <summary>Scenare poruch: upravit, spustit, zastavit.</summary>
    public const string ScenariosManage = "scenarios.manage";

    /// <summary>Vlastne vzhlady tabul.</summary>
    public const string DesignsManage = "designs.manage";

    /// <summary>Nahravat, nahrat, mazat a prehravat zaznamy komunikacie.</summary>
    public const string RecordingsManage = "recordings.manage";

    /// <summary>Virtualne tabule a ich rozlozenia, import z INISSView.</summary>
    public const string VirtualManage = "virtual.manage";

    /// <summary>Informacne panely: spravy, playlisty, skupiny, panely a ich subory.</summary>
    public const string SignageManage = "signage.manage";

    /// <summary>Nastavenie servera vratane HTTPS, databazy a oznameni.</summary>
    public const string SettingsManage = "settings.manage";

    /// <summary>Zaloha a obnova simulatora, restart servera.</summary>
    public const string ServerManage = "server.manage";

    /// <summary>Spravovat pouzivatelov a roly.</summary>
    public const string UsersManage = "users.manage";

    /// <summary>Spravovat pristupove kluce zariadeni a programov.</summary>
    public const string KeysManage = "keys.manage";

    /// <summary>Vsetky opravnenia.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        SimulatorView, LogView, ArchiveView, StatisticsView, AuditView, LinesManage, BoardsControl, ScenariosManage, DesignsManage, RecordingsManage,
        VirtualManage, SignageManage, SettingsManage, ServerManage, UsersManage, KeysManage
    ];
}

/// <summary>Politiky s viacerymi opravneniami - staci jedno z nich.</summary>
public static class Policies
{
    /// <summary>Zoznam roli: pouzivatelia alebo pristupove kluce.</summary>
    public const string RolesView = "roles.view";

    /// <summary>Ako server bezi: nastavenie alebo zaloha a restart.</summary>
    public const string ServerView = "server.view";

    /// <summary>Politiky a ich opravnenia.</summary>
    public static IReadOnlyDictionary<string, string[]> AnyOf { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [RolesView] = [Permissions.UsersManage, Permissions.KeysManage],
        [ServerView] = [Permissions.SettingsManage, Permissions.ServerManage]
    };
}
