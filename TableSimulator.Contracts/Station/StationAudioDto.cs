namespace TableSimulator.Contracts.Station;

/// <summary>Audio linka servera INISS2 z Audio.xml, ktora hlasi cez ustrednu AK Signal.</summary>
/// <param name="Name">nazov audio linky (<c>jm</c>)</param>
/// <param name="Station">cislo stanice (<c>st</c>)</param>
/// <param name="Channel">kanal ustredne AK Signal UH 1 alebo 2; pri AKSIGNAL 1</param>
public sealed record StationAudioDto(string Name, int Station, int Channel = 1);
