using TableSimulator.Core.Boards.History;

namespace TableSimulator.Application.Devices;

/// <summary>Historia ustredni AK Signal v databaze.</summary>
public interface IDeviceHistoryRepository
{
    /// <summary>Zapise odobrate zmeny.</summary>
    Task AddAsync(DeviceHistorySample sample, CancellationToken cancellationToken);

    /// <summary>
    /// Historia v obdobi [<paramref name="fromMs" />, <paramref name="toMs" />) aj so stavom na jeho zaciatku; linka a adresa
    /// null = vsetky ustredne.
    /// </summary>
    Task<DeviceHistoryData> QueryAsync(long fromMs, long toMs, int? line, int? address, CancellationToken cancellationToken);

    /// <summary>Odstrani historiu starsiu nez <paramref name="timeMs" /> (posledny stav kazdej ustredne ostava); vrati pocet zaznamov.</summary>
    Task<int> DeleteOlderThanAsync(long timeMs, CancellationToken cancellationToken);
}
