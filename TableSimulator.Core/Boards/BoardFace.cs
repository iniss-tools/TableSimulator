using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards;

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

/// <summary>
/// Miesto okolo matice pre prvky cela (v bodoch).
/// </summary>
/// <param name="Left">vlavo</param>
/// <param name="Top">hore</param>
/// <param name="Right">vpravo</param>
/// <param name="Bottom">dole</param>
public sealed record FaceMargin(int Left = 0, int Top = 0, int Right = 0, int Bottom = 0);

/// <summary>
/// Prvok cela: plocha, text alebo hodiny. Poloha je voci ukotveniu - zarovnanie urcuje, od ktoreho okraja ukotvenia
/// sa meria (<see cref="Align" /> vlavo od laveho, vpravo od praveho, na stred od stredu; zvislo rovnako).
/// </summary>
public sealed record FaceElement
{
    /// <summary>Druh prvku.</summary>
    public FaceElementKind Kind { get; init; } = FaceElementKind.Text;

    /// <summary>K comu je prvok ukotveny.</summary>
    public FaceAnchor Anchor { get; init; } = FaceAnchor.Face;

    /// <summary>Riadok tabule zhora od 1 pri ukotveni k riadku.</summary>
    public int Row { get; init; } = 1;

    /// <summary>Vodorovna vzdialenost od okraja ukotvenia podla zarovnania.</summary>
    public double X { get; init; }

    /// <summary>Zvisla vzdialenost od okraja ukotvenia podla zvisleho zarovnania; zaporna = mimo ukotvenia (napr. nad riadkom).</summary>
    public double Y { get; init; }

    /// <summary>Sirka plochy; 0 = po okraj ukotvenia, zaporna = o tolko kratsia.</summary>
    public double Width { get; init; }

    /// <summary>Vyska plochy; 0 = po okraj ukotvenia, zaporna = o tolko nizsia.</summary>
    public double Height { get; init; }

    /// <summary>Vodorovne zarovnanie.</summary>
    public FaceAlign Align { get; init; } = FaceAlign.Left;

    /// <summary>Zvisle zarovnanie.</summary>
    public FaceVAlign VAlign { get; init; } = FaceVAlign.Top;

    /// <summary>Text; riadky oddelene <c>\n</c>, premenne <c>{kluc}</c>.</summary>
    public string Text { get; init; } = "";

    /// <summary>Velkost pisma prveho riadku (aj cislic hodin).</summary>
    public double Size { get; init; } = 8;

    /// <summary>Velkost pisma dalsich riadkov (napr. preklady); 0 = ako prvy.</summary>
    public double Size2 { get; init; }

    /// <summary>Tucne pismo; null = podla vzhladu.</summary>
    public bool? Bold { get; init; }

    /// <summary>Pomenovany styl textu zo vzhladu (<c>title</c>, <c>label</c>, <c>number</c>); prazdny = ostatne texty cela.</summary>
    public string Style { get; init; } = "";

    /// <summary>Farba textu, cislic hodin alebo okraja plochy: <c>#rrggbb</c> alebo nazov farby vzhladu; prazdna = predvolena.</summary>
    public string Color { get; init; } = "";

    /// <summary>Vypln plochy alebo pozadie hodin: <c>#rrggbb</c> alebo nazov farby vzhladu; prazdna = bez vyplne (hodiny predvolene).</summary>
    public string Fill { get; init; } = "";

    /// <summary>Hrubka okraja plochy; 0 = bez okraja.</summary>
    public double Border { get; init; }

    /// <summary>Zaoblenie rohov plochy.</summary>
    public double Radius { get; init; }

    /// <summary>Najvacsia sirka textu - dlhsi sa zuzi; 0 = bez obmedzenia.</summary>
    public double MaxWidth { get; init; }

    /// <summary>Rozostup pismen.</summary>
    public double Spacing { get; init; }

    /// <summary>Format hodin.</summary>
    public FaceClockFormat Format { get; init; } = FaceClockFormat.HourMinutePadded;

    /// <summary>Hodiny pismom prehliadaca, alebo bodmi ako matica tabule.</summary>
    public FaceRender Render { get; init; } = FaceRender.Text;
}

/// <summary>Druh prvku cela.</summary>
public enum FaceElementKind
{
    /// <summary>Plocha - pas, panel, ramik.</summary>
    [JsonStringEnumMemberName("rect")] Rect,

    /// <summary>Text.</summary>
    [JsonStringEnumMemberName("text")] Text,

    /// <summary>Hodiny tabule.</summary>
    [JsonStringEnumMemberName("clock")] Clock
}

/// <summary>Ukotvenie prvku cela.</summary>
public enum FaceAnchor
{
    /// <summary>Cela plocha vo vnutri ramu.</summary>
    [JsonStringEnumMemberName("face")] Face,

    /// <summary>Matica (vsetky riadky).</summary>
    [JsonStringEnumMemberName("matrix")] Matrix,

    /// <summary>Riadok tabule (<see cref="FaceElement.Row" />).</summary>
    [JsonStringEnumMemberName("row")] Row,

    /// <summary>Miesto nad maticou (sirka matice).</summary>
    [JsonStringEnumMemberName("top")] Top,

    /// <summary>Miesto pod maticou (sirka matice).</summary>
    [JsonStringEnumMemberName("bottom")] Bottom,

    /// <summary>Miesto vlavo od matice (cela vyska).</summary>
    [JsonStringEnumMemberName("left")] Left,

    /// <summary>Miesto vpravo od matice (cela vyska).</summary>
    [JsonStringEnumMemberName("right")] Right
}

/// <summary>Vodorovne zarovnanie prvku cela.</summary>
public enum FaceAlign
{
    /// <summary>Lavy okraj prvku od laveho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("left")] Left,

    /// <summary>Stred prvku od stredu ukotvenia.</summary>
    [JsonStringEnumMemberName("center")] Center,

    /// <summary>Pravy okraj prvku od praveho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("right")] Right
}

/// <summary>Zvisle zarovnanie prvku cela.</summary>
public enum FaceVAlign
{
    /// <summary>Horny okraj prvku od horneho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("top")] Top,

    /// <summary>Stred prvku od stredu ukotvenia.</summary>
    [JsonStringEnumMemberName("middle")] Middle,

    /// <summary>Spodny okraj prvku od spodneho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("bottom")] Bottom
}

/// <summary>Ako sa kreslia hodiny cela.</summary>
public enum FaceRender
{
    /// <summary>Pismom prehliadaca.</summary>
    [JsonStringEnumMemberName("text")] Text,

    /// <summary>Bodmi ako matica tabule (LED, LCD) - samostatny maly modul.</summary>
    [JsonStringEnumMemberName("display")] Display
}

/// <summary>Format hodin cela.</summary>
public enum FaceClockFormat
{
    /// <summary>9:05</summary>
    [JsonStringEnumMemberName("H:mm")] HourMinute,

    /// <summary>09:05</summary>
    [JsonStringEnumMemberName("HH:mm")] HourMinutePadded,

    /// <summary>09:05:30</summary>
    [JsonStringEnumMemberName("HH:mm:ss")] WithSeconds
}
