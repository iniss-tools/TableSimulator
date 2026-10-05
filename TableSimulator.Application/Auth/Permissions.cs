namespace TableSimulator.Application.Auth;

/// <summary>Opravnenia - nazvy su zaroven nazvy politik autorizacie a kluce pre webove rozhranie.</summary>
public static class Permissions
{
    /// <summary>Vidiet linky, tabule, zaznamy, pisma, vzhlady a nastavenie.</summary>
    public const string SimulatorView = "simulator.view";

    /// <summary>Vidiet zaznam komunikacie.</summary>
    public const string LogView = "log.view";

    /// <summary>Menit linky a tabule, poruchy, mazat zaznam komunikacie, importovat stanicu a projekty Monitora.</summary>
    public const string SimulatorEdit = "simulator.edit";

    /// <summary>Nahravat, mazat a prehravat zaznamy komunikacie.</summary>
    public const string RecordingsManage = "recordings.manage";

    /// <summary>Menit nastavenie servera.</summary>
    public const string SettingsManage = "settings.manage";

    /// <summary>Spravovat pouzivatelov.</summary>
    public const string UsersManage = "users.manage";

    /// <summary>Vsetky opravnenia.</summary>
    public static IReadOnlyList<string> All { get; } = [SimulatorView, LogView, SimulatorEdit, RecordingsManage, SettingsManage, UsersManage];
}
