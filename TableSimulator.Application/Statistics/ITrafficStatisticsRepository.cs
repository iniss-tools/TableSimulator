using TableSimulator.Core.Statistics;

namespace TableSimulator.Application.Statistics;

/// <summary>Statistika komunikacie v databaze.</summary>
public interface ITrafficStatisticsRepository
{
    /// <summary>Zapise odobraty vzorku; minuta, ktora uz v databaze je, sa pripocita.</summary>
    Task AddAsync(TrafficSample sample, CancellationToken cancellationToken);

    /// <summary>
    /// Intervaly po <paramref name="bucketMinutes" /> minutach v obdobi [<paramref name="fromMinute" />,
    /// <paramref name="toMinute" />) a zmeny stavu liniek v nom aj posledna pred nim; <paramref name="line" /> null = vsetky linky.
    /// </summary>
    Task<TrafficData> QueryAsync(long fromMinute, long toMinute, int bucketMinutes, int? line, CancellationToken cancellationToken);

    /// <summary>Odstrani statistiku starsiu nez <paramref name="minute" />; vrati pocet odstranenych zaznamov.</summary>
    Task<int> DeleteOlderThanAsync(long minute, CancellationToken cancellationToken);
}
