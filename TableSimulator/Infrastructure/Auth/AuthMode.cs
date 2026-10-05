namespace TableSimulator.Infrastructure.Auth;

/// <summary>Rezim prihlasovania urceny pri starte servera (zmena nastavenia sa prejavi po restarte).</summary>
/// <param name="LoginRequired">vyzaduje sa prihlasenie; inak je kazda poziadavka (server pocuva len na tomto pocitaci) spravca</param>
public sealed record AuthMode(bool LoginRequired);
