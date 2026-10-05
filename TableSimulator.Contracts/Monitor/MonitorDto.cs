namespace TableSimulator.Contracts.Monitor;

/// <summary>Instalacia ELEN Monitora pre webove rozhranie.</summary>
/// <param name="Available">priecinok Monitora existuje</param>
public sealed record MonitorDto(bool Available, string Folder, BoardTemplateDto[] Panels, string[] Projects, MonitorLookDto Look);
