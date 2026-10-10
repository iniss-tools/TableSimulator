namespace TableSimulator.Contracts.Database;

/// <summary>Vysledok skusky pripojenia k PostgreSQL.</summary>
/// <param name="Ok">server odpovedal a prihlasenie preslo</param>
/// <param name="Error">chyba pripojenia (text od servera alebo Npgsql); null = v poriadku</param>
/// <param name="Version">verzia servera PostgreSQL</param>
/// <param name="Exists">databaza existuje; ak nie, server ju pri starte zalozi (pouzivatel potrebuje pravo CREATEDB)</param>
/// <param name="CanCreate">pouzivatel smie zakladat databazy</param>
/// <param name="Initialized">databaza uz obsahuje tabulky simulatora</param>
public sealed record DatabaseTestResultDto(bool Ok, string? Error, string? Version, bool Exists, bool CanCreate, bool Initialized);
