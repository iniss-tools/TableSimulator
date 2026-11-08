namespace TableSimulator.Core.Boards.AkSignal;

/// <summary>
/// Audio linka servera z Audio.xml, ktora hlasi cez ustrednu.
/// </summary>
/// <param name="Name">nazov audio linky (<c>jm</c>)</param>
/// <param name="Station">cislo stanice (<c>st</c>)</param>
public sealed record AudioCircuit(string Name, int Station)
{
    /// <summary>Kanal ustredne AKSIGNALUH 1 alebo 2 (<c>ch</c>); pri AKSIGNAL sa neberie do uvahy.</summary>
    public int Channel { get; init; } = 1;
}
