namespace TableSimulator.Application.Abstractions.Results;

/// <summary>Druh chyby - podla neho server zvoli stav HTTP.</summary>
public enum ErrorType
{
    /// <summary>Necakana chyba (500).</summary>
    Failure,

    /// <summary>Neplatne vstupy (400 so zoznamom chyb).</summary>
    Validation,

    /// <summary>Poziadavku nemozno vykonat (400).</summary>
    Problem,

    /// <summary>Nieco neexistuje (404).</summary>
    NotFound,

    /// <summary>Koliduje so stavom (409).</summary>
    Conflict
}
