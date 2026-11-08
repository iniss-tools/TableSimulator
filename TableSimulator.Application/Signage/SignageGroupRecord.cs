namespace TableSimulator.Application.Signage;

/// <summary>Skupina panelov v databaze; obsah (<see cref="Contracts.Signage.SetSignageGroupRequest" />) ako JSON.</summary>
public sealed record SignageGroupRecord(int Id, string Name, string Definition);
