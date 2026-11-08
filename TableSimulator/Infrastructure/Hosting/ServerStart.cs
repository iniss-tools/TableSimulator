namespace TableSimulator.Infrastructure.Hosting;

/// <summary>Ako dopadlo spustenie servera sluzby.</summary>
/// <param name="Url">adresa bieziaceho servera; null = nespustil sa</param>
/// <param name="Error">dovod; null = server sa nespustil v case a dovod nie je znamy</param>
public sealed record ServerStart(string? Url, string? Error);
