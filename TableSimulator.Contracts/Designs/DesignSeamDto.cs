namespace TableSimulator.Contracts.Designs;

/// <summary>Svy medzi modulmi tabule (napr. LCD panely) - kde su, urcuje sirka modulu v cele tabule.</summary>
/// <param name="Color">farba (#rrggbb); null = bez svov</param>
/// <param name="Width">hrubka ako podiel vzdialenosti bodov</param>
public sealed record DesignSeamDto(string? Color, double Width);
