namespace TableSimulator.Application.Signage;

/// <summary>Panel v databaze.</summary>
public sealed record SignagePanelRecord(int Id, string Name, string Note, int? GroupId);
