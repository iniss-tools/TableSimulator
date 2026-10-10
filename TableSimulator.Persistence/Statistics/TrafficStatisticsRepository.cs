using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Statistics;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Statistics;

namespace TableSimulator.Persistence.Statistics;

/// <summary>Minutova statistika komunikacie a zmeny stavu liniek v databaze.</summary>
public sealed class TrafficStatisticsRepository(AppDbContext db) : ITrafficStatisticsRepository
{
    /// <summary>Najdlhsi popis zmeny stavu (adresa INISSu, chyba portu).</summary>
    private const int MaxDetail = 200;

    public async Task AddAsync(TrafficSample sample, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sample);
        foreach (var minute in sample.Lines)
        {
            if (await db.LineTraffic.FindAsync([minute.Line, minute.Minute], cancellationToken).ConfigureAwait(false) is { } existing)
                existing.Add(minute);
            else
                db.LineTraffic.Add(LineTrafficRecord.From(minute));
        }

        foreach (var minute in sample.Boards)
        {
            if (await db.BoardTraffic.FindAsync([minute.Line, minute.Address, minute.Minute], cancellationToken).ConfigureAwait(false) is { } existing)
                existing.Add(minute);
            else
                db.BoardTraffic.Add(BoardTrafficRecord.From(minute));
        }

        foreach (var change in sample.StatusChanges)
        {
            var detail = change.Detail is { Length: > MaxDetail } text ? text[..MaxDetail] : change.Detail;
            db.LineStatuses.Add(new LineStatusRecord(0, change.Line, change.Time.ToUnixTimeMilliseconds(), SimulatorConfigJson.Key(change.Status), detail));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public async Task<TrafficData> QueryAsync(long fromMinute, long toMinute, int bucketMinutes, int? line, CancellationToken cancellationToken)
    {
        var bucket = (long)Math.Max(1, bucketMinutes);
        var lineQuery = db.LineTraffic.AsNoTracking().Where(r => r.Minute >= fromMinute && r.Minute < toMinute);
        var boardQuery = db.BoardTraffic.AsNoTracking().Where(r => r.Minute >= fromMinute && r.Minute < toMinute);
        var fromMs = fromMinute * 60_000;
        var toMs = toMinute * 60_000;
        var changeQuery = db.LineStatuses.AsNoTracking().Where(s => s.TimeMs >= fromMs && s.TimeMs < toMs);
        // stav na zaciatku obdobia = posledna zmena pred nim
        var beforeQuery = db.LineStatuses.AsNoTracking()
            .Where(s => s.TimeMs < fromMs && s.TimeMs == db.LineStatuses.Where(x => x.Line == s.Line && x.TimeMs < fromMs).Max(x => x.TimeMs));
        if (line is { } number)
        {
            lineQuery = lineQuery.Where(r => r.Line == number);
            boardQuery = boardQuery.Where(r => r.Line == number);
            changeQuery = changeQuery.Where(s => s.Line == number);
            beforeQuery = beforeQuery.Where(s => s.Line == number);
        }

        var lines = await lineQuery.GroupBy(r => new { r.Line, Bucket = r.Minute / bucket })
            .Select(g => new LineTrafficRecord
            {
                Line = g.Key.Line, Minute = g.Key.Bucket * bucket, Ok = g.Sum(r => r.Ok), BadChecksum = g.Sum(r => r.BadChecksum),
                NoBoard = g.Sum(r => r.NoBoard), Noise = g.Sum(r => r.Noise), Other = g.Sum(r => r.Other), Replies = g.Sum(r => r.Replies),
                Faults = g.Sum(r => r.Faults), BytesIn = g.Sum(r => r.BytesIn), BytesOut = g.Sum(r => r.BytesOut),
                Connections = g.Sum(r => r.Connections), Disconnections = g.Sum(r => r.Disconnections)
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var boards = await boardQuery.GroupBy(r => new { r.Line, r.Address, Bucket = r.Minute / bucket })
            .Select(g => new BoardTrafficRecord
            {
                Line = g.Key.Line, Address = g.Key.Address, Minute = g.Key.Bucket * bucket, Frames = g.Sum(r => r.Frames), Acks = g.Sum(r => r.Acks),
                Naks = g.Sum(r => r.Naks), Unanswered = g.Sum(r => r.Unanswered), MaxGapMs = g.Max(r => r.MaxGapMs)
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var changes = await beforeQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
        changes.AddRange(await changeQuery.ToListAsync(cancellationToken).ConfigureAwait(false));
        return new TrafficData(lines, boards, changes);
    }

    public async Task<int> DeleteOlderThanAsync(long minute, CancellationToken cancellationToken)
    {
        var ms = minute * 60_000;
        var deleted = await db.LineTraffic.Where(r => r.Minute < minute).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        deleted += await db.BoardTraffic.Where(r => r.Minute < minute).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        // posledna zmena stavu kazdej linky ostava - je to stav linky na zaciatku uchovanej statistiky
        deleted += await db.LineStatuses
            .Where(s => s.TimeMs < ms && s.TimeMs < db.LineStatuses.Where(x => x.Line == s.Line && x.TimeMs < ms).Max(x => x.TimeMs))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        return deleted;
    }
}
