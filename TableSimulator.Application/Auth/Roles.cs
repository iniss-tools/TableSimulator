namespace TableSimulator.Application.Auth;

/// <summary>
/// Vstavane roly: spravca smie vzdy vsetko (aj opravnenia pridane neskor), operator a pozorovatel su upravitelne
/// predvolene roly. Dalsie roly si zaklada spravca (<see cref="RoleCatalog" />).
/// </summary>
public static class Roles
{
    public const string Admin = "admin";
    public const string Operator = "operator";
    public const string Viewer = "viewer";

    /// <summary>Najdlhsi nazov roly.</summary>
    public const int MaxDisplayName = 40;

    /// <summary>Vstavane roly od najvyssej.</summary>
    public static IReadOnlyList<string> BuiltIn { get; } = [Admin, Operator, Viewer];

    /// <summary>Rola je vstavana (neda sa zmazat; spravca ani upravit).</summary>
    public static bool IsBuiltIn(string? role) => role is Admin or Operator or Viewer;

    /// <summary>Predvolene opravnenia vstavanej roly (pri prvom starte a po aktualizacii zo starych pevnych roli).</summary>
    public static IReadOnlyList<string> DefaultsOf(string? role) => role switch
    {
        Admin => Permissions.All,
        Operator =>
        [
            Permissions.SimulatorView, Permissions.LogView, Permissions.ArchiveView, Permissions.StatisticsView, Permissions.LinesManage,
            Permissions.BoardsControl, Permissions.ScenariosManage, Permissions.DesignsManage, Permissions.RecordingsManage, Permissions.VirtualManage,
            Permissions.SignageManage
        ],
        Viewer => [Permissions.SimulatorView, Permissions.LogView, Permissions.ArchiveView, Permissions.StatisticsView],
        _ => []
    };
}
