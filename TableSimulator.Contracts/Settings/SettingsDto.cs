namespace TableSimulator.Contracts.Settings;

/// <summary>Nastavenie servera pre webove rozhranie.</summary>
/// <param name="ConfigPath">subor s linkami a tabulami (len na citanie)</param>
public sealed record SettingsDto(int HttpPort, bool AllowNetwork, string FontsFolder, string DesignsFolder, string ConnectHost, string ConfigPath)
{
    /// <summary>Priecinok programu ELEN Monitor.</summary>
    public string MonitorFolder { get; init; } = "";
}
