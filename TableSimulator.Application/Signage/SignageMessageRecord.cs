namespace TableSimulator.Application.Signage;

/// <summary>Sprava v databaze; obsah (<see cref="Contracts.Signage.SetSignageMessageRequest" />) ako JSON.</summary>
public sealed record SignageMessageRecord(int Id, string Name, string Definition);
