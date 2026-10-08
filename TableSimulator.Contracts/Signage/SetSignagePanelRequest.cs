namespace TableSimulator.Contracts.Signage;

/// <summary>Panel - monitor, ktory otvara <c>#/panel/{id}</c> (POST /api/signage/panels, PUT /api/signage/panels/{id}).</summary>
/// <param name="Name">nazov</param>
/// <param name="Note">poznamka (umiestnenie)</param>
/// <param name="Group">skupina; null = mimo prevadzky</param>
public sealed record SetSignagePanelRequest(string Name, string Note, int? Group);
