namespace TableSimulator.Contracts.Station;

/// <summary>INISS2: ustredna AK Signal na linke - svetla z Lights.xml a audio linky z Audio.xml s jej adresou.</summary>
/// <param name="Address">adresa ustredne 1-255; AKSIGNALUH adresu nema, v simulatore 0</param>
/// <param name="Manufacturer">kluc vyrobcu v simulatore (<c>AKSIGNAL</c>, <c>AKSIGNALUH</c>)</param>
/// <param name="Name">nazov pre obsluhu - nazvy svetiel a audio liniek</param>
/// <param name="Lights">svetla</param>
/// <param name="Audio">audio linky</param>
public sealed record InissControllerDto(int Address, string Manufacturer, string Name, IReadOnlyList<StationLightDto> Lights, IReadOnlyList<StationAudioDto> Audio);
