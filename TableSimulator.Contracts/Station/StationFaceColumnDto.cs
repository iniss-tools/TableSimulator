using TableSimulator.Contracts.Faces;

namespace TableSimulator.Contracts.Station;

/// <summary>Stlpec katalogovej tabule zobrazeny pri vlaku - nad nim byva na cele natlaceny popis.</summary>
/// <param name="Line">riadok zaznamu od 0</param>
/// <param name="Start">zaciatok stlpca v bodoch</param>
/// <param name="End">koniec stlpca v bodoch (prvy bod za stlpcom)</param>
/// <param name="Align">zarovnanie textu v stlpci</param>
/// <param name="Content">druh obsahu (cislo FILL_SECTION z katalogu)</param>
public sealed record StationFaceColumnDto(int Line, int Start, int End, FaceAlign Align, int Content);
