using TableSimulator.Contracts.Archive;
using TableSimulator.Core.Archive;

namespace TableSimulator.Application.Mapping;

/// <summary>Denne archivy INISSu.</summary>
public static partial class DtoMapper
{
    public static TableArchiveEntryDto ToDto(TableArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new TableArchiveEntryDto(entry.Time, entry.Station, ToDto(entry.Train), entry.Text,
            entry.Tables.Select(t => new TableArchiveTableDto(t.Names, t.Columns.Select(c => new TableArchiveColumnDto(c.Name, c.Value)).ToArray(),
                t.Logical, t.Row)).ToArray());
    }

    public static ReportArchiveEntryDto ToDto(ReportArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new ReportArchiveEntryDto(entry.Time, entry.Kind, entry.Name, entry.Auto, entry.Station, ToDto(entry.Train), entry.ArrivalTrack,
            entry.DepartureTrack, entry.ArrivalDelay, entry.DepartureDelay, entry.Text, entry.Result,
            entry.Details.Select(d => new ArchiveDetailDto(d.Key, d.Value)).ToArray());
    }

    private static ArchiveTrainDto? ToDto(ArchiveTrain? train) => train is null ? null : new ArchiveTrainDto(train.Type, train.Number, train.Name, train.Date);
}
