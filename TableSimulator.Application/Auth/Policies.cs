namespace TableSimulator.Application.Auth;

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
