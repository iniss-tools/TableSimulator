namespace TableSimulator.Application.Audit;

/// <summary>
/// Prikaz vie, cim sa tyka (cislo linky, tabula, pouzivatel…) - ciel zaznamu auditu. Implementovat explicitne, aby
/// sa ciel nezapisal do podrobnosti dvakrat.
/// </summary>
public interface IAuditable
{
    /// <summary>Ciel akcie.</summary>
    string AuditTarget { get; }
}
