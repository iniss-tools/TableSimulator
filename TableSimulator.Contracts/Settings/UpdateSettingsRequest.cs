namespace TableSimulator.Contracts.Settings;

/// <summary>Zmena nastavenia servera (PUT /api/settings).</summary>
public sealed record UpdateSettingsRequest
{
    public int HttpPort { get; init; }
    public bool AllowNetwork { get; init; }
    public string FontsFolder { get; init; } = "";
    public string DesignsFolder { get; init; } = "";
    public string ConnectHost { get; init; } = "";
    public string MonitorFolder { get; init; } = "";
    public bool RequireLogin { get; init; }
    public IReadOnlyList<string>? VirtualFolders { get; init; }
}
