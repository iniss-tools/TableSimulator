namespace TableSimulator.Contracts.Monitor;

/// <summary>Import projektu ELEN Monitora na linku (POST /api/monitor/import).</summary>
public sealed record MonitorImportRequest(string Project, int Line);
