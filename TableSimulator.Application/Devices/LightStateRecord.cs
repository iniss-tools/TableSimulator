using TableSimulator.Core.Boards.History;

namespace TableSimulator.Application.Devices;

/// <summary>Zmena stavu svetla ustredne AK Signal v databaze (cas v ms od 1. 1. 1970 UTC, stav ako bity <see cref="LightFlags" />).</summary>
public sealed record LightStateRecord(long Id, int Line, int Address, long TimeMs, int Flags);
