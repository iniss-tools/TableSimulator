namespace TableSimulator.Contracts.Settings;

/// <summary>Vysledok odoslania cez jeden kanal.</summary>
/// <param name="Configured">kanal je nastaveny (inak sa neposielalo)</param>
/// <param name="Error">chyba; null = odoslane</param>
public sealed record AlertChannelResultDto(bool Configured, string? Error);
