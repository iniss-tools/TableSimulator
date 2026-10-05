using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Archive;

namespace TableSimulator.Application.Archive;

/// <summary>Stranka archivu hlaseni jedneho dna s vyberom podla vlaku a textu.</summary>
/// <param name="Date">den</param>
/// <param name="Train">cislo vlaku (cast cisla); prazdne = vsetky</param>
/// <param name="Text">hladany text (bez ohladu na diakritiku); prazdny = vsetko</param>
/// <param name="Newest">od najnovsich zaznamov</param>
/// <param name="Offset">kolko vyhovujucich zaznamov preskocit</param>
/// <param name="Limit">pocet zaznamov na stranke</param>
public sealed record GetReportArchiveQuery(DateOnly Date, string? Train, string? Text, bool Newest, int Offset, int Limit) : IQuery<ReportArchivePageDto>;
