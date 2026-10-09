namespace TableSimulator.Contracts.Recordings;

/// <summary>
/// Porovnanie dvoch zaznamov (GET /api/recordings/compare): co INISS ukazoval na tabuliach - postupnosti textu kazdej
/// tabule porovnane ako <c>diff</c>; na case nezalezi, len na poradi.
/// </summary>
public sealed record RecordingComparisonDto(RecordingSideDto A, RecordingSideDto B, IReadOnlyList<BoardComparisonDto> Boards);
