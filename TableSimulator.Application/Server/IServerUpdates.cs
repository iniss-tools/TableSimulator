using TableSimulator.Contracts.Server;

namespace TableSimulator.Application.Server;

/// <summary>
/// Nova verzia servera z update.json posledneho vydania (ToolsCore.Iniss.Updates). Pravidelne ju zistuje sluzba servera,
/// posledny vysledok drzi v pamati. Server mimo vydania (bez adresy indexu) nezistuje nic.
/// </summary>
public interface IServerUpdates
{
    /// <summary>Posledny vysledok.</summary>
    ServerUpdateDto Status { get; }

    /// <summary>Zisti novu verziu hned; chybu siete alebo indexu vrati vo vysledku.</summary>
    Task<ServerUpdateDto> CheckAsync(CancellationToken cancellationToken);
}
