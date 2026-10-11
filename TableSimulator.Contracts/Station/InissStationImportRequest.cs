namespace TableSimulator.Contracts.Station;

/// <summary>Volby nacitania instalacie INISSu z nastavenia (POST /api/station/iniss).</summary>
/// <param name="FaceLanguage">jazyk popisov na celach novych tabul</param>
/// <param name="FacesForExisting">celo dostanu aj tabule, ktore uz v simulatore su a celo nemaju</param>
public sealed record InissStationImportRequest(StationFaceLanguage FaceLanguage = StationFaceLanguage.Sk, bool FacesForExisting = false);
