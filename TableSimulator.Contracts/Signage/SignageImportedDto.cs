namespace TableSimulator.Contracts.Signage;

/// <summary>Vysledok obnovy zalohy informacnych panelov (POST /api/signage/import).</summary>
/// <param name="Messages">obnovene alebo pridane spravy</param>
/// <param name="Playlists">playlisty</param>
/// <param name="Groups">skupiny</param>
/// <param name="Panels">panely</param>
/// <param name="Media">obnovene nahrate subory (pri pridani len tie, ktore v simulatore este neboli)</param>
/// <param name="Replaced">vsetko sa nahradilo zalohou (cisla zaznamov ostali); inak sa obsah pridal s novymi cislami</param>
public sealed record SignageImportedDto(int Messages, int Playlists, int Groups, int Panels, int Media, bool Replaced);
