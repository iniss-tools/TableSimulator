namespace TableSimulator.Contracts.Faces;

/// <summary>
/// Prvok cela: plocha, text, hodiny, symbol alebo obrazok. Zarovnanie urcuje, od ktoreho okraja ukotvenia sa meria poloha (vlavo od
/// laveho, vpravo od praveho, na stred od stredu; zvislo rovnako). Rozmery a polohy v bodoch matice.
/// </summary>
public sealed record FaceElementDto
{
    /// <summary>Druh prvku.</summary>
    public FaceElementKind Kind { get; init; } = FaceElementKind.Text;

    /// <summary>K comu je prvok ukotveny.</summary>
    public FaceAnchor Anchor { get; init; } = FaceAnchor.Face;

    /// <summary>Riadok tabule zhora od 1 pri ukotveni k riadku.</summary>
    public int Row { get; init; } = 1;

    /// <summary>Vodorovna vzdialenost od okraja ukotvenia.</summary>
    public double X { get; init; }

    /// <summary>Zvisla vzdialenost od okraja ukotvenia; zaporna = mimo neho (napr. nad riadkom).</summary>
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

    /// <summary>Velkost pisma dalsich riadkov; 0 = ako prvy.</summary>
    public double Size2 { get; init; }

    /// <summary>Tucne pismo; null = podla vzhladu.</summary>
    public bool? Bold { get; init; }

    /// <summary>Pomenovany styl textu zo vzhladu (<c>title</c>, <c>label</c>, <c>number</c>); prazdny = ostatne texty cela.</summary>
    public string Style { get; init; } = "";

    /// <summary>Farba textu, cislic hodin alebo okraja plochy: <c>#rrggbb</c> alebo nazov farby vzhladu; prazdna = predvolena.</summary>
    public string Color { get; init; } = "";

    /// <summary>Vypln plochy alebo pozadie hodin: <c>#rrggbb</c> alebo nazov farby vzhladu; prazdna = bez vyplne.</summary>
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

    /// <summary>Symbol prvku druhu symbol (stvorec velkosti <see cref="Size" />).</summary>
    public FaceSymbol Symbol { get; init; } = FaceSymbol.ArrowUp;

    /// <summary>Obrazok prvku druhu obrazok ako data URL <c>data:image/…;base64,…</c>; prazdny = bez obrazka.</summary>
    public string Image { get; init; } = "";

    /// <summary>Zamknuty prvok - v editore sa na tabuli neda vybrat ani posunut; kreslenie nemeni.</summary>
    public bool Locked { get; init; }
}
