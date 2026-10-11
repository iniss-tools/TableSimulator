namespace TableSimulator.Contracts.Designs;

/// <summary>Pomenovany styl textu cela; null = ako ostatne texty cela.</summary>
/// <param name="Color">farba (#rrggbb)</param>
/// <param name="Font">pismo (CSS font-family)</param>
/// <param name="Bold">tucne</param>
public sealed record DesignTextStyleDto(string? Color, string? Font, bool? Bold);
