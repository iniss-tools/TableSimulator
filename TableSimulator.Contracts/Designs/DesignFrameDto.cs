namespace TableSimulator.Contracts.Designs;

/// <summary>Ram tabule.</summary>
/// <param name="Width">sirka v CSS px pri mierke 1</param>
/// <param name="Color">farba (#rrggbb)</param>
/// <param name="Radius">polomer zaoblenia v CSS px</param>
public sealed record DesignFrameDto(double Width, string Color, double Radius);
