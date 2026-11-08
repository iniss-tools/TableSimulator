namespace TableSimulator.Core.Simulation.Events;

/// <summary>Linka zmenila stav alebo nastavenie.</summary>
public sealed record LineChanged(int Line) : SimulatorEvent;
