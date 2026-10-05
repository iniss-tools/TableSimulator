namespace TableSimulator.Contracts.Settings;

/// <summary>Vysledok zmeny nastavenia.</summary>
/// <param name="Restart">zmena sa prejavi az po restarte servera (port, pristup zo siete, prihlasovanie, priecinok vzhladov)</param>
public sealed record SettingsUpdatedDto(bool Restart);
