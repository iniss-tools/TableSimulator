namespace TableSimulator.Core.Boards.Faces;

/// <summary>
/// Celo tabule - co je na tabuli natlacene okolo matice: plochy, popisy, panely s cislom a hodiny. Je to predloha
/// zdielana tabulami rovnakeho typu; texty mozu obsahovat premenne <c>{track}</c>, <c>{platform}</c>, <c>{station}</c>
/// a <c>{name}</c> (nazov tabule), ich hodnoty ma tabula (<see cref="BoardProfile.FaceValues" />). INISS celo neposiela
/// - patri k hardveru tabule. Polohy a rozmery su v bodoch matice.
/// </summary>
public sealed record BoardFace
{
    /// <summary>Kluc predlohy (male pismena bez diakritiky, cislice, pomlcka).</summary>
    public string Id { get; init; } = "";

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Miesto okolo matice pre prvky cela.</summary>
    public FaceMargin Margin { get; init; } = new();

    /// <summary>Medzera medzi riadkami tabule; null = podla vzhladu.</summary>
    public int? RowGap { get; init; }

    /// <summary>Pocet riadkov v skupine (napr. riadky zaznamu na samostatnom module); 0 = bez skupin.</summary>
    public int RowGroup { get; init; }

    /// <summary>Medzera navyse medzi skupinami riadkov.</summary>
    public int GroupGap { get; init; }

    /// <summary>Sirka modulu v bodoch - na hraniciach modulov su svy (farbu ma vzhlad); 0 = bez svov.</summary>
    public int ModuleWidth { get; init; }

    /// <summary>Prvky cela v poradi kreslenia.</summary>
    public IReadOnlyList<FaceElement> Elements { get; init; } = [];
}
