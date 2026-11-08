namespace TableSimulator.Application.Devices;

/// <summary>Co dotaz na historiu vrati z databazy.</summary>
/// <param name="Lights">zmeny stavu svetla v obdobi a posledna pred nim pri kazdej ustredni</param>
/// <param name="Announcements">hlasenia, ktore v obdobi hrali</param>
public sealed record DeviceHistoryData(IReadOnlyList<LightStateRecord> Lights, IReadOnlyList<AnnouncementRecord> Announcements);
