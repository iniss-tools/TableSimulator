namespace TableSimulator.Application.Auth;

/// <summary>
/// Pevne roly a ich opravnenia: spravca smie vsetko, operator ovlada simulator, pozorovatel len pozera.
/// </summary>
public static class Roles
{
    public const string Admin = "admin";
    public const string Operator = "operator";
    public const string Viewer = "viewer";

    private static readonly Dictionary<string, string[]> Map = new(StringComparer.Ordinal)
    {
        [Admin] = [.. Permissions.All],
        [Operator] = [Permissions.SimulatorView, Permissions.LogView, Permissions.SimulatorEdit, Permissions.RecordingsManage],
        [Viewer] = [Permissions.SimulatorView, Permissions.LogView]
    };

    /// <summary>Vsetky roly od najvyssej.</summary>
    public static IReadOnlyList<string> All { get; } = [Admin, Operator, Viewer];

    /// <summary>Opravnenia roly; neznama rola nema ziadne.</summary>
    public static IReadOnlyList<string> PermissionsOf(string? role) => role is not null && Map.TryGetValue(role, out var p) ? p : [];

    /// <summary>Roly, ktore opravnenie maju.</summary>
    public static IReadOnlyList<string> WithPermission(string permission) => All.Where(r => Map[r].Contains(permission)).ToArray();
}
