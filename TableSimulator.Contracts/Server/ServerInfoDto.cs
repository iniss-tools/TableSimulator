namespace TableSimulator.Contracts.Server;

/// <summary>Ako server bezi.</summary>
/// <param name="Service">server bezi ako sluzba Windows (da sa restartovat z weboveho rozhrania)</param>
/// <param name="Version">verzia servera</param>
/// <param name="DataFolder">priecinok nastaveni, databazy a logov</param>
public sealed record ServerInfoDto(bool Service, string Version, string DataFolder);
