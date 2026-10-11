namespace TableSimulator.Contracts.Designs;

/// <summary>Ram tabule.</summary>
/// <param name="Width">sirka v CSS px pri mierke 1</param>
/// <param name="Color">farba (#rrggbb)</param>
/// <param name="Radius">polomer zaoblenia v CSS px</param>
/// <param name="Style">plochy alebo kovovy (hlinikovy profil s leskom)</param>
/// <param name="Badge">stitok vyrobcu vpravo dole na rame; prazdny = bez stitku</param>
public sealed record DesignFrameDto(double Width, string Color, double Radius, DesignFrameStyle Style = DesignFrameStyle.Flat, string Badge = "");
