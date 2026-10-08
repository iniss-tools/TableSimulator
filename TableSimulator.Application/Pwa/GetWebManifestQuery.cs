using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Pwa;

namespace TableSimulator.Application.Pwa;

/// <summary>Manifest aplikacie kiosku.</summary>
/// <param name="View">stranka kiosku (/view/&lt;linka-adresa&gt;, /wall, /virtual/&lt;cislo&gt;)</param>
/// <param name="Name">nazov aplikacie (pre obsluhu)</param>
/// <param name="Key">pristupovy kluc zariadenia - pojde do adresy po spusteni; null = bez kluca</param>
public sealed record GetWebManifestQuery(string View, string? Name, string? Key) : IQuery<WebManifestDto>;
