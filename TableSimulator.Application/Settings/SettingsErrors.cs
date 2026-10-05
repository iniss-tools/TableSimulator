namespace TableSimulator.Application.Settings;

/// <summary>Kody chyb nastavenia servera.</summary>
public static class SettingsErrors
{
    public const string Port = "settings.port";
    public const string ConnectHost = "settings.connectHost";
    public const string VirtualFolders = "settings.virtualFolders";
    public const string HttpsPort = "settings.httpsPort";

    /// <summary>Najviac sledovanych priecinkov s XML tabul.</summary>
    public const int MaxVirtualFolders = 4;
}
